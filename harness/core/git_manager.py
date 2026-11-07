
import re
import subprocess
from pathlib import Path


class GitManager:
    def __init__(self, project_dir: Path):
        self.project_dir = project_dir.resolve()

    def _git(self, *args: str) -> str:
        result = subprocess.run(
            ["git", *args],
            cwd=self.project_dir,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )

        if result.returncode != 0:
            raise RuntimeError(
                f"Git 명령 실패: {' '.join(args)}\n"
                f"{result.stderr}"
            )

        return result.stdout.strip()

    def current_branch(self) -> str:
        return self._git("branch", "--show-current")

    def has_changes(self) -> bool:
        return bool(self._git("status", "--porcelain"))

    def create_branch(self, task: str, prefix: str = "feature") -> str:
        if self.has_changes():
            raise RuntimeError(
                "미커밋 변경 사항이 있어 브랜치를 생성할 수 없습니다."
            )

        slug = re.sub(
            r"[^a-z0-9]+",
            "-",
            task.lower(),
        ).strip("-")

        if not slug:
            slug = "unity-task"

        slug = slug[:40].rstrip("-")
        branch = f"{prefix}/{slug}"

        self._git("switch", "-c", branch)
        return branch

    def commit(self, message: str) -> None:
        if not self.has_changes():
            print("커밋할 변경 사항 없음")
            return

        self._git("add", "-A")
        self._git("diff", "--cached", "--check")
        self._git("commit", "-m", message)

    def push(self) -> None:
        branch = self.current_branch()

        if branch in {"main", "master"}:
            raise RuntimeError(
                "main/master 브랜치에는 직접 Push할 수 없습니다."
            )

        self._git("push", "-u", "origin", branch)
