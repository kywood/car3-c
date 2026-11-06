
import json
from datetime import datetime, timezone
from pathlib import Path


class ReviewManager:
    def __init__(self, base_dir: Path):
        self.path = base_dir / "reports" / "review.json"
        self.path.parent.mkdir(parents=True, exist_ok=True)

    def request(self, task: str) -> None:
        """검증이 필요한 작업 등록."""
        data = {
            "task": task,
            "status": "pending",
            "created_at": datetime.now(
                timezone.utc
            ).isoformat(),
        }
        self._save(data)

    def approve(self, task: str) -> bool:
        """사용자 승인."""
        data = self._load()

        if data.get("task") != task:
            return False

        if data.get("status") != "pending":
            return False

        data["status"] = "approved"
        self._save(data)
        return True

    def reject(self, task: str) -> bool:
        """사용자 거절."""
        data = self._load()

        if data.get("task") != task:
            return False

        if data.get("status") != "pending":
            return False

        data["status"] = "rejected"
        self._save(data)
        return True

    def get_status(self, task: str) -> str | None:
        data = self._load()

        if data.get("task") != task:
            return None

        return data.get("status")

    def _save(self, data: dict) -> None:
        tmp_path = self.path.with_suffix(".tmp")
        tmp_path.write_text(
            json.dumps(
                data,
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )
        tmp_path.replace(self.path)

    def _load(self) -> dict:
        if not self.path.exists():
            return {}

        return json.loads(
            self.path.read_text(encoding="utf-8")
        )
