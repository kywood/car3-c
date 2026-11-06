
import shutil
import subprocess
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path


@dataclass
class ValidationResult:
    success: bool
    returncode: int
    output: str
    log_path: Path | None = None


class UnityValidator:
    def __init__(self, project_dir: Path):
        self.project_dir = project_dir.resolve()
        self.log_dir = (
            self.project_dir / "harness" / "reports" / "builds"
        )
        self.log_dir.mkdir(parents=True, exist_ok=True)

    @staticmethod
    def to_windows_path(path: Path) -> str:
        result = subprocess.run(
            ["wslpath", "-w", str(path)],
            capture_output=True,
            text=True,
            check=True,
        )
        return result.stdout.strip()

    def validate(self, timeout: int = 1200) -> ValidationResult:
        # 1. Unity 프로젝트 구조 확인
        for folder in ("Assets", "Packages", "ProjectSettings"):
            if not (self.project_dir / folder).is_dir():
                return ValidationResult(
                    success=False,
                    returncode=-1,
                    output=f"Unity 프로젝트 폴더 없음: {folder}",
                )

        # 2. Unity CLI 확인
        unity_cmd = (
            shutil.which("unity.exe")
            or shutil.which("unity")
        )

        if not unity_cmd:
            return ValidationResult(
                success=False,
                returncode=-1,
                output="Unity CLI를 찾을 수 없습니다.",
            )

        # 3. Windows 경로 변환
        windows_project = self.to_windows_path(self.project_dir)

        build_dir = self.project_dir / "Builds" / "Android"
        build_dir.mkdir(parents=True, exist_ok=True)

        apk_path = build_dir / "car3-c.apk"
        windows_apk = self.to_windows_path(apk_path)

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        log_path = self.log_dir / f"unity_build_{timestamp}.log"

        # 4. Unity Android 빌드 명령
        command = [
            unity_cmd,
            "build",
            windows_project,
            "--profile",
            "Android",
            "--output-path",
            windows_apk,
            "--allow-dirty-build",
            "--non-interactive",
            "--timeout",
            str(timeout),
        ]

        print("=== Unity Android Build ===", flush=True)
        print(f"Project: {self.project_dir}", flush=True)
        print(f"Output: {apk_path}", flush=True)

        # 이전 APK를 빌드 성공으로 오인하지 않도록 제거
        try:
            apk_path.unlink(missing_ok=True)
        except OSError as e:
            return ValidationResult(
                success=False,
                returncode=-1,
                output=f"기존 APK 삭제 실패: {e}",
                log_path=log_path,
            )

        # 5. 빌드 실행
        try:
            result = subprocess.run(
                command,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                timeout=timeout + 30,
            )

        except subprocess.TimeoutExpired:
            return ValidationResult(
                success=False,
                returncode=-1,
                output="Unity 빌드 시간 초과",
                log_path=log_path,
            )

        except OSError as e:
            return ValidationResult(
                success=False,
                returncode=-1,
                output=f"Unity CLI 실행 실패: {e}",
                log_path=log_path,
            )

        # Windows 한글 로그 인코딩 처리
        output = result.stdout.decode(
            "utf-8", errors="replace"
        )

        if "\ufffd" in output:
            output = result.stdout.decode(
                "cp949", errors="replace"
            )

        # 6. 빌드 로그 저장
        log_path.write_text(output, encoding="utf-8")

        print(output[-3000:], flush=True)

        # 7. 성공 여부 판정
        success = (
            result.returncode == 0
            and apk_path.is_file()
            and apk_path.stat().st_size > 0
        )

        if success:
            print("Unity Android 빌드 성공", flush=True)
        else:
            print("Unity Android 빌드 실패", flush=True)

        return ValidationResult(
            success=success,
            returncode=result.returncode,
            output=output,
            log_path=log_path,
        )
