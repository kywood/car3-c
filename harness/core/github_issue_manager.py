import hashlib
import json
import os
import re
import subprocess
import urllib.error
import urllib.request
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path


TASK_MARKER = "harness-task"
ISSUE_REF_PATTERN = re.compile(r"\(#(\d+)\)")


@dataclass
class Issue:
    number: int
    url: str
    title: str
    created: bool = False


def task_key(task: str) -> str:
    """요청 원문(앞의 (#번호) 표기 제외)으로 만든 고유 키. 같은 요청으로 이슈가 중복 생성되지 않게 한다."""
    text = ISSUE_REF_PATTERN.sub("", task).strip()
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:12]


def issue_number_in(task: str) -> int | None:
    """TODO 문장에 '(#12)'처럼 이슈 번호가 있으면 돌려준다."""
    match = ISSUE_REF_PATTERN.search(task)
    return int(match.group(1)) if match else None


def repo_from_url(url: str) -> str:
    match = re.search(r"github\.com[:/]+([^/]+/[^/]+?)(?:\.git)?/?$", url.strip())
    if not match:
        raise RuntimeError(f"GitHub 저장소 주소가 아닙니다: {url}")
    return match.group(1)


def issue_title(task: str, limit: int = 60) -> str:
    text = ISSUE_REF_PATTERN.sub("", task).strip().splitlines()[0].strip()
    if len(text) > limit:
        text = text[:limit].rstrip() + "…"
    return f"[기능] {text}"


def issue_body(task: str, source: str, key: str) -> str:
    quoted = "\n".join(f"> {line}" if line.strip() else ">" for line in task.strip().splitlines())
    return (
        "## 요청 원문\n"
        f"{quoted}\n\n"
        "## 출처\n"
        f"- {source}\n"
        f"- 등록 시각: {datetime.now().strftime('%Y-%m-%d %H:%M')}\n\n"
        "## 진행 규칙\n"
        "- 이 이슈 전용 브랜치 `feature/issue-<번호>-...`에서 작업한다.\n"
        "- 완료 커밋에 `Closes #<번호>`를 넣어 main 머지 시 이슈가 닫히게 한다.\n"
        "- main 머지는 사용자 승인 후 진행한다.\n\n"
        f"<!-- {TASK_MARKER}:{key} -->\n"
    )


class GitHubIssueManager:
    """하네스 작업(사용자 요청)마다 GitHub 이슈를 만들고 찾는다. 표준 라이브러리만 사용한다."""

    def __init__(self, project_dir: Path, label: str = "harness"):
        self.project_dir = project_dir.resolve()
        self.label = label
        self.repo = repo_from_url(self._git("remote", "get-url", "origin"))
        self._token: str | None = None

    def _git(self, *args: str, stdin: str | None = None, env: dict | None = None) -> str:
        result = subprocess.run(
            ["git", *args],
            cwd=self.project_dir,
            input=stdin,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            env=env,
            timeout=60,
        )
        if result.returncode != 0:
            raise RuntimeError(f"Git 명령 실패: {' '.join(args)}\n{result.stderr}")
        return result.stdout.strip()

    def _token_value(self) -> str:
        if self._token:
            return self._token
        token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
        if not token:
            # Git에 저장된 GitHub 인증(Git Credential Manager)을 묻지 않고 읽는다.
            env = dict(os.environ, GIT_TERMINAL_PROMPT="0", GCM_INTERACTIVE="never")
            output = self._git("credential", "fill", stdin="protocol=https\nhost=github.com\n\n", env=env)
            fields = dict(line.split("=", 1) for line in output.splitlines() if "=" in line)
            token = fields.get("password")
        if not token:
            raise RuntimeError("GitHub 인증을 찾을 수 없습니다. GITHUB_TOKEN을 설정하거나 Git 인증을 저장하세요.")
        self._token = token
        return token

    def _request(self, method: str, path: str, payload: dict | None = None):
        data = json.dumps(payload).encode("utf-8") if payload is not None else None
        request = urllib.request.Request(
            f"https://api.github.com{path}",
            data=data,
            method=method,
            headers={
                "Authorization": f"Bearer {self._token_value()}",
                "Accept": "application/vnd.github+json",
                "User-Agent": "car3c-harness",
                "Content-Type": "application/json",
            },
        )
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                body = response.read()
                return json.loads(body) if body else None
        except urllib.error.HTTPError as error:
            detail = error.read().decode("utf-8", "replace")[:300]
            raise RuntimeError(f"GitHub API 실패 {method} {path}: {error.code} {detail}") from error

    def _ensure_label(self) -> None:
        try:
            self._request("POST", f"/repos/{self.repo}/labels", {
                "name": self.label,
                "color": "1f6feb",
                "description": "Unity AI Harness 작업 요청",
            })
        except RuntimeError as error:
            if " 422 " not in str(error):
                raise

    def get_issue(self, number: int) -> Issue:
        data = self._request("GET", f"/repos/{self.repo}/issues/{number}")
        return Issue(number=data["number"], url=data["html_url"], title=data["title"])

    def _registry_path(self) -> Path:
        return self.project_dir / "harness" / "reports" / "issues.json"

    def _load_registry(self) -> dict:
        path = self._registry_path()
        if not path.exists():
            return {}
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return {}

    def _remember(self, task: str, number: int) -> None:
        # GitHub 목록 조회는 방금 만든 이슈를 몇 초간 반영하지 않으므로 로컬에도 기록한다.
        registry = self._load_registry()
        registry[task_key(task)] = number
        path = self._registry_path()
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(registry, ensure_ascii=False, indent=1), encoding="utf-8")

    def find_issue(self, task: str) -> Issue | None:
        number = issue_number_in(task)
        if number is not None:
            return self.get_issue(number)

        remembered = self._load_registry().get(task_key(task))
        if remembered is not None:
            return self.get_issue(int(remembered))

        marker = f"{TASK_MARKER}:{task_key(task)}"
        page = 1
        while True:
            items = self._request(
                "GET",
                f"/repos/{self.repo}/issues?state=all&labels={self.label}&per_page=100&page={page}",
            )
            matches = [item for item in items if marker in (item.get("body") or "")]
            if matches:
                item = min(matches, key=lambda i: i["number"])
                self._remember(task, item["number"])
                return Issue(number=item["number"], url=item["html_url"], title=item["title"])
            if len(items) < 100:
                return None
            page += 1

    def ensure_issue(self, task: str, source: str) -> Issue:
        existing = self.find_issue(task)
        if existing is not None:
            return existing

        self._ensure_label()
        data = self._request("POST", f"/repos/{self.repo}/issues", {
            "title": issue_title(task),
            "body": issue_body(task, source, task_key(task)),
            "labels": [self.label],
        })
        self._remember(task, data["number"])
        return Issue(number=data["number"], url=data["html_url"], title=data["title"], created=True)

    def close_as_duplicate(self, number: int, original: int) -> None:
        self.comment(number, f"#{original} 과 중복되어 닫습니다.")
        self._request("PATCH", f"/repos/{self.repo}/issues/{number}", {"state": "closed", "state_reason": "not_planned"})

    def comment(self, number: int, text: str) -> None:
        self._request("POST", f"/repos/{self.repo}/issues/{number}/comments", {"body": text})
