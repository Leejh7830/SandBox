[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

try {
    $rawInput = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($rawInput)) {
        Write-Output '{"decision":"allow"}'
        exit 0
    }

    $payload = $rawInput | ConvertFrom-Json
    $toolName = $payload.toolCall.name
    $argsObj = $payload.toolCall.args

    # 1. Check write_file / edit_file targeting real _meta user data
    if ($toolName -eq "write_file" -or $toolName -eq "edit_file") {
        $targetPath = [string]$argsObj.Path
        if ($targetPath -match "(?i)[\\/]_meta[\\/](WorkLog\.json|WorkLog_Images|sandbox[\\/])") {
            $resp = @{
                decision = "deny"
                reason   = "[🔒 데이터 보호 훅 작동] 실제 사용자 데이터(_meta/WorkLog.json 및 _meta/sandbox/)는 AI가 직접 수정하거나 덮어쓸 수 없도록 보호되어 있습니다."
            } | ConvertTo-Json -Compress
            Write-Output $resp
            exit 0
        }
    }

    # 2. Check run_command attempting destructive operations on _meta user data
    if ($toolName -eq "run_command") {
        $cmdLine = [string]$argsObj.CommandLine
        $hitsMeta = $cmdLine -match "(?i)_meta[\\/](WorkLog\.json|WorkLog_Images|sandbox)"
        $isDestructive = $cmdLine -match "(?i)\b(Remove-Item|rm\b|del\b|erase\b|Clear-Content|Set-Content|Out-File|ri\b|rd\b|rmdir\b)"
        if ($hitsMeta -and $isDestructive) {
            $resp = @{
                decision = "deny"
                reason   = "[🔒 데이터 보호 훅 작동] 명령어에 실제 사용자 데이터(_meta/) 삭제·초기화 시도가 감지되어 자동 차단되었습니다."
            } | ConvertTo-Json -Compress
            Write-Output $resp
            exit 0
        }

        # 3. Multi-Window Build & Capture Interlock (Wait if another window is compiling/capturing)
        if ($cmdLine -match "(?i)(MSBuild\.exe|csc\.exe|__temp_capture)") {
            for ($i = 0; $i -lt 22; $i++) {
                $busy = Get-Process -Name "MSBuild", "csc", "__temp_capture" -ErrorAction SilentlyContinue
                if (-not $busy) { break }
                Start-Sleep -Seconds 1
            }
        }
    }

    Write-Output '{"decision":"allow"}'
}
catch {
    Write-Output '{"decision":"allow"}'
}
