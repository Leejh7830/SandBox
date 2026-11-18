using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GateHelper.EcoSim.Services
{
    /// <summary>
    /// SandBox\9. EcoSim\Resources\*.wav 트랙을 순차 로테이션 재생(없을 경우 내장 중세 포크 합성 루프 폴백)하는 Zero-Resource BGM 서비스.
    /// </summary>
    public sealed class EcoBgmService : IDisposable
    {
        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        private readonly List<WavTrack> _tracks = new List<WavTrack>();
        private readonly Timer _nextTrackTimer;

        private SoundPlayer _player;
        private MemoryStream _fallbackStream;
        private int _currentIndex;
        private bool _isPlaying;
        private bool _initialized;
        private bool _disposed;

        private struct WavTrack
        {
            public string FilePath;
            public int DurationMs;
        }

        public bool IsMuted { get; private set; }
        public int VolumePercent { get; private set; } = 65;

        public string CurrentTrackName
        {
            get
            {
                if (_tracks.Count > 0 && _currentIndex >= 0 && _currentIndex < _tracks.Count)
                    return Path.GetFileNameWithoutExtension(_tracks[_currentIndex].FilePath);
                return "내장 중세 포크 루프";
            }
        }

        public EcoBgmService()
        {
            _nextTrackTimer = new Timer();
            _nextTrackTimer.Tick += (s, e) => AdvanceToNextTrack();
            ApplyWaveVolume(VolumePercent);
        }

        public void SetVolume(int percent)
        {
            VolumePercent = Math.Max(0, Math.Min(100, percent));
            ApplyWaveVolume(VolumePercent);
        }

        public void NextTrack(bool isTabVisible)
        {
            if (_disposed || _tracks.Count <= 1) return;
            _currentIndex = (_currentIndex + 1) % _tracks.Count;
            if (isTabVisible && !IsMuted)
            {
                PlayCurrent();
            }
        }

        private static void ApplyWaveVolume(int percent)
        {
            try
            {
                ushort vol = (ushort)((percent / 100.0) * 0xFFFF);
                uint packed = ((uint)vol << 16) | vol;
                waveOutSetVolume(IntPtr.Zero, packed);
            }
            catch { }
        }

        public void SetMuted(bool muted, bool isTabVisible)
        {
            IsMuted = muted;
            UpdatePlayback(isTabVisible);
        }

        public void UpdatePlayback(bool isTabVisible)
        {
            if (_disposed) return;

            bool shouldPlay = isTabVisible && !IsMuted;
            if (shouldPlay && !_isPlaying)
            {
                EnsureInitialized();
                ApplyWaveVolume(VolumePercent);
                PlayCurrent();
            }
            else if (!shouldPlay && _isPlaying)
            {
                StopInternal();
            }
        }

        private void AdvanceToNextTrack()
        {
            _nextTrackTimer.Stop();
            if (_disposed || !_isPlaying || _tracks.Count <= 1) return;

            _currentIndex = (_currentIndex + 1) % _tracks.Count;
            PlayCurrent();
        }

        private void PlayCurrent()
        {
            if (_tracks.Count > 0)
            {
                var track = _tracks[_currentIndex];
                _player?.Stop();
                _player?.Dispose();
                _player = new SoundPlayer(track.FilePath);
                _player.Load();

                if (_tracks.Count == 1)
                {
                    _player.PlayLooping();
                    _nextTrackTimer.Stop();
                }
                else
                {
                    _player.Play();
                    _nextTrackTimer.Interval = Math.Max(3000, track.DurationMs + 250);
                    _nextTrackTimer.Start();
                }
                _isPlaying = true;
                return;
            }

            // 폴백: 내장 합성 루프
            if (_player == null)
            {
                byte[] wavBytes = SynthesizeAmbientLoopWav();
                _fallbackStream = new MemoryStream(wavBytes, writable: false);
                _player = new SoundPlayer(_fallbackStream);
                _player.Load();
            }
            _fallbackStream.Position = 0;
            _player.PlayLooping();
            _isPlaying = true;
        }

        private void StopInternal()
        {
            _nextTrackTimer.Stop();
            _player?.Stop();
            _isPlaying = false;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            string dir = ResolveResourcesDirectory();
            if (dir != null && Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir, "*.wav");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < files.Length; i++)
                {
                    int durationMs = ReadWavDurationMs(files[i]);
                    _tracks.Add(new WavTrack
                    {
                        FilePath = files[i],
                        DurationMs = durationMs
                    });
                }
            }
        }

        private static string ResolveResourcesDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "SandBox", "9. EcoSim", "Resources"),
                Path.Combine(baseDir, "..", "..", "SandBox", "9. EcoSim", "Resources")
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                string full = Path.GetFullPath(candidates[i]);
                if (Directory.Exists(full)) return full;
            }
            return null;
        }

        private static int ReadWavDurationMs(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var br = new BinaryReader(fs))
                {
                    if (fs.Length < 44) return 30000;
                    fs.Position = 12;

                    int byteRate = 0;
                    int dataSize = 0;

                    while (fs.Position + 8 <= fs.Length)
                    {
                        string chunkId = new string(br.ReadChars(4));
                        int chunkSize = br.ReadInt32();
                        if (chunkSize < 0) break;

                        if (chunkId == "fmt " && chunkSize >= 16 && fs.Position + 16 <= fs.Length)
                        {
                            br.ReadInt16(); // audioFormat
                            br.ReadInt16(); // channels
                            br.ReadInt32(); // sampleRate
                            byteRate = br.ReadInt32();
                            fs.Position += chunkSize - 12;
                        }
                        else if (chunkId == "data")
                        {
                            dataSize = chunkSize;
                            break;
                        }
                        else
                        {
                            fs.Position += chunkSize;
                        }
                    }

                    if (byteRate > 0 && dataSize > 0)
                    {
                        return (int)((dataSize * 1000L) / byteRate);
                    }
                }
            }
            catch { }
            return 30000;
        }

        /// <summary>
        /// 외부 WAV 파일이 없을 때 사용하는 내장 중세 포크 16초 루프 폴백.
        /// </summary>
        private static byte[] SynthesizeAmbientLoopWav()
        {
            const int sampleRate = 22050;
            const int seconds = 16;
            const int totalSamples = sampleRate * seconds;
            const double twoPi = Math.PI * 2.0;

            double[][] luteChords =
            {
                new[] { 196.00, 293.66, 392.00, 493.88 },
                new[] { 146.83, 220.00, 293.66, 369.99 },
                new[] { 164.81, 246.94, 329.63, 392.00 },
                new[] { 130.81, 196.00, 261.63, 329.63 },
                new[] { 196.00, 293.66, 392.00, 493.88 },
                new[] { 130.81, 196.00, 261.63, 329.63 },
                new[] { 146.83, 220.00, 293.66, 369.99 },
                new[] { 196.00, 293.66, 392.00, 493.88 }
            };

            double[] fluteMelody =
            {
                392.00, 493.88, 587.33, 493.88,  739.99, 659.25, 587.33, 440.00,
                493.88, 587.33, 659.25, 587.33,  523.25, 493.88, 440.00, 392.00,
                392.00, 440.00, 493.88, 587.33,  523.25, 493.88, 440.00, 392.00,
                440.00, 587.33, 523.25, 440.00,  392.00, 493.88, 392.00, 0.00
            };

            byte[] wav = new byte[44 + totalSamples * 2];
            WriteWavHeader(wav, sampleRate, totalSamples);

            const int chordSamples = sampleRate * 2;
            const int strumStep = sampleRate / 4;
            const int noteSamples = sampleRate / 2;

            for (int i = 0; i < totalSamples; i++)
            {
                double t = i / (double)sampleRate;
                int chordIdx = Math.Min(luteChords.Length - 1, i / chordSamples);
                int strumIdx = (i % chordSamples) / strumStep;
                double strumT = (i % strumStep) / (double)sampleRate;
                double[] ch = luteChords[chordIdx];

                int[] pattern = { 0, 1, 2, 3, 2, 1, 3, 2 };
                double luteFreq = ch[pattern[strumIdx % pattern.Length]];
                double luteEnv = Math.Exp(-strumT * 6.5) * Math.Min(1.0, strumT * 180.0);
                double lute =
                    (Math.Sin(twoPi * luteFreq * strumT) * 0.65 +
                     Math.Sin(twoPi * luteFreq * 2.0 * strumT) * 0.25 * Math.Exp(-strumT * 3.0) +
                     Math.Sin(twoPi * luteFreq * 3.0 * strumT) * 0.10 * Math.Exp(-strumT * 6.0)) * luteEnv * 0.28;

                double bassT = (i % chordSamples) / (double)sampleRate;
                double bassFreq = ch[0] * 0.5;
                double bassEnv = Math.Exp(-bassT * 2.8) * Math.Min(1.0, bassT * 120.0);
                double bass = (Math.Sin(twoPi * bassFreq * bassT) + 0.22 * Math.Sin(twoPi * bassFreq * 2.0 * bassT)) * bassEnv * 0.16;

                int noteIdx = Math.Min(fluteMelody.Length - 1, i / noteSamples);
                double fluteFreq = fluteMelody[noteIdx];
                double flute = 0.0;
                if (fluteFreq > 1.0)
                {
                    double noteT = (i % noteSamples) / (double)sampleRate;
                    double vibrato = 1.0 + Math.Sin(twoPi * 5.2 * t) * 0.0035;
                    double f = fluteFreq * vibrato;
                    double fluteEnv = Math.Min(1.0, noteT * 14.0) * Math.Min(1.0, (0.5 - noteT) * 10.0);
                    flute =
                        (Math.Sin(twoPi * f * noteT) * 0.78 +
                         Math.Sin(twoPi * f * 2.0 * noteT) * 0.14 +
                         Math.Sin(twoPi * f * 3.0 * noteT) * 0.05) * Math.Max(0.0, fluteEnv) * 0.20;
                }

                double mix = (lute + bass + flute) * 0.72;
                if (i < 440) mix *= (i / 440.0);
                else if (i > totalSamples - 440) mix *= ((totalSamples - i) / 440.0);

                short pcm = (short)Math.Max(-32767, Math.Min(32767, (int)(mix * 32767.0)));
                int offset = 44 + i * 2;
                wav[offset] = (byte)(pcm & 0xFF);
                wav[offset + 1] = (byte)((pcm >> 8) & 0xFF);
            }

            return wav;
        }

        private static void WriteWavHeader(byte[] buf, int sampleRate, int sampleCount)
        {
            int dataSize = sampleCount * 2;
            int fileSize = 36 + dataSize;
            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(buf, 0);
            BitConverter.GetBytes(fileSize).CopyTo(buf, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(buf, 8);
            BitConverter.GetBytes(16).CopyTo(buf, 16);
            BitConverter.GetBytes((short)1).CopyTo(buf, 20);
            BitConverter.GetBytes((short)1).CopyTo(buf, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(buf, 24);
            BitConverter.GetBytes(sampleRate * 2).CopyTo(buf, 28);
            BitConverter.GetBytes((short)2).CopyTo(buf, 32);
            BitConverter.GetBytes((short)16).CopyTo(buf, 34);
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(buf, 36);
            BitConverter.GetBytes(dataSize).CopyTo(buf, 40);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _nextTrackTimer.Stop();
            _nextTrackTimer.Dispose();
            _player?.Stop();
            _player?.Dispose();
            _fallbackStream?.Dispose();
        }
    }
}
