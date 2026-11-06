
import subprocess
from pathlib import Path


class CodingAgent:
    def __init__(self, project_dir: Path):
        self.project_dir = project_dir

    def execute(self, task: str) -> bool:
        prompt = f"""
너는 Unity C# 전문 개발 에이전트다.

프로젝트: car3-c

현재 TODO:
{task}

작업 규칙:
1. 기존 Unity 프로젝트 구조를 먼저 분석한다.
2. 이미 구현된 기능은 중복 개발하지 않는다.
3. 필요한 코드만 수정한다.
4. 작업 내용을 한국어로 요약한다.
5. Unity 프로젝트 외부 파일은 변경하지 않는다.
6. 이번 실행에서는 Git 커밋을 하지 않는다.

위 작업을 수행하라.
"""

        print(f"Codex 작업 시작: {task}")

        result = subprocess.run(
            [
                "codex",
                "exec",
                "--full-auto",
                "-C",
                str(self.project_dir),
                prompt,
            ],
            capture_output=True,
            text=True,
            errors="replace",
            timeout=900,
        )

        print(result.stdout)

        if result.returncode != 0:
            print(result.stderr)
            return False

        return True
