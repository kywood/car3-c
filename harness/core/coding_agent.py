
import os
import queue
import re
import shutil
import signal
import subprocess
import threading
import time

from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Literal, Callable


Status = Literal[
    "success",
    "usage_limit",
    "timeout",
    "failed",
]


@dataclass
class AgentResult:
    status: Status
    returncode: int
    log_path: Path


class CodingAgent:
    """Windows Claude Code를 실행하는 AI 코딩 에이전트."""

    def __init__(self, project_dir: Path):
        self.project_dir = project_dir.resolve()

        self.log_dir = (
            self.project_dir / "harness" / "reports" / "logs"
        )
        self.log_dir.mkdir(parents=True, exist_ok=True)

    def execute(
        self,
        task: str,
        timeout: int = 900,
        on_progress: Callable | None = None,
        report_interval: int = 300,
    ) -> AgentResult:

        claude_cmd = shutil.which("claude.exe")

        if not claude_cmd:
            raise RuntimeError(
                "Windows Claude Code를 찾을 수 없습니다."
            )

        prompt = f"""
너는 Unity C# 전문 개발 에이전트다.

프로젝트: car3-c

현재 작업:
{task}

작업 규칙:
1. 프로젝트 루트의 CLAUDE.md 지침을 준수한다.
2. 기존 Unity 프로젝트 구조를 먼저 분석한다.
3. 이미 구현된 기능은 중복 개발하지 않는다.
4. 필요한 코드만 최소한으로 수정한다.
5. 기존 기능을 훼손하지 않는다.
6. 가능한 범위에서 테스트를 실행한다.
7. Git 커밋은 하지 않는다.
8. Unity 프로젝트 외부 파일은 변경하지 않는다.
9. 작업 결과를 한국어로 요약한다.

작업을 수행하라.
"""

        command = [
            claude_cmd,
            "-p",
            "--permission-mode",
            "acceptEdits",
            "--output-format",
            "text",
            prompt,
        ]

        log_path = self.log_dir / (
            f"claude_{datetime.now():%Y%m%d_%H%M%S}.log"
        )

        print(
            f"Claude Code 작업 시작: {task}",
            flush=True,
        )

        process = subprocess.Popen(
            command,
            cwd=self.project_dir,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            encoding="utf-8",
            errors="replace",
            bufsize=1,
            start_new_session=True,
        )

        output_queue = queue.Queue()

        def read_output():
            try:
                for line in process.stdout:
                    output_queue.put(line)
            finally:
                output_queue.put(None)

        reader = threading.Thread(
            target=read_output,
            daemon=True,
        )
        reader.start()

        usage_limit = False
        timed_out = False
        reader_finished = False

        deadline = time.monotonic() + timeout
        next_report = time.monotonic() + report_interval

        try:
            with log_path.open(
                "w", encoding="utf-8"
            ) as log:

                while True:
                    now = time.monotonic()

                    # 실행 시간 제한
                    if now >= deadline:
                        timed_out = True
                        break

                    # 중간 Slack 진행 보고
                    if (
                        on_progress is not None
                        and now >= next_report
                    ):
                        elapsed = int(timeout - (deadline - now))

                        try:
                            on_progress(task, elapsed)
                        except Exception as e:
                            print(f"진행 보고 오류: {e}")

                        next_report = (
                            time.monotonic() + report_interval
                        )

                    # Claude 출력 수집
                    try:
                        line = output_queue.get(timeout=0.5)
                    except queue.Empty:
                        if (
                            reader_finished
                            and process.poll() is not None
                        ):
                            break
                        continue

                    if line is None:
                        reader_finished = True

                        if process.poll() is not None:
                            break

                        continue

                    print(line, end="", flush=True)
                    log.write(line)
                    log.flush()

                    # 사용량 제한 감지
                    if re.search(
                        r"usage limit|rate limit|"
                        r"you.ve reached your limit|"
                        r"out of extra usage|"
                        r"try again at",
                        line,
                        re.IGNORECASE,
                    ):
                        usage_limit = True

        finally:
            if process.poll() is None:
                try:
                    os.killpg(process.pid, signal.SIGTERM)
                    process.wait(timeout=5)
                except (ProcessLookupError, subprocess.TimeoutExpired):
                    if process.poll() is None:
                        os.killpg(process.pid, signal.SIGKILL)
                        process.wait()

            if process.stdout:
                process.stdout.close()

        returncode = process.returncode

        if timed_out:
            status = "timeout"
        elif usage_limit:
            status = "usage_limit"
        elif returncode == 0:
            status = "success"
        else:
            status = "failed"

        print(
            f"Claude Code 종료: {status}",
            flush=True,
        )

        return AgentResult(
            status=status,
            returncode=returncode,
            log_path=log_path,
        )
