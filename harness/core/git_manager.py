
import hashlib
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

    def branch_for_task(
        self,
        task: str,
        prefix: str = "feature",
    ) -> str:
        slug = re.sub(
            r"[^a-z0-9]+",
            "-",
            task.lower(),
        ).strip("-")[:30].rstrip("-")

        if not slug:
            slug = "unity-task"

        task_hash = hashlib.sha256(
            task.encode("utf-8")
        ).hexdigest()[:8]

        return f"{prefix}/{slug}-{task_hash}"

    def prepare_branch(self, branch: str) -> str:
        current = self.current_branch()

        if current == branch:
            return branch

        if self.has_changes():
            raise RuntimeError(
                "미커밋 변경 사항이 있어 브랜치를 전환할 수 없습니다."
            )

        local_branches = self._git(
            "branch", "--format=%(refname:short)"
        ).splitlines()

        if branch in local_branches:
            self._git("switch", branch)
        else:
            self._git("switch", "-c", branch)

        return branch

    def commit(self, message: str) -> bool:
        branch = self.current_branch()

        if branch in {"main", "master"}:
            raise RuntimeError(
                "기본 브랜치에서는 커밋할 수 없습니다."
            )

        if not self.has_changes():
            return False

        self._git("add", "-A")
        self._git("diff", "--cached", "--check")
        self._git("commit", "-m", message)
        return True

    def push(self) -> None:
        branch = self.current_branch()

        if branch in {"main", "master"}:
            raise RuntimeError(
                "기본 브랜치에는 Push할 수 없습니다."
            )

        self._git("push", "-u", "origin", branch)
