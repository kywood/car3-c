
from dataclasses import dataclass
from pathlib import Path
import re


@dataclass
class Todo:
    line_number: int
    description: str
    completed: bool


class TodoManager:
    def __init__(self, path: Path):
        self.path = path

    def get_todos(self) -> list[Todo]:
        lines = self.path.read_text(encoding="utf-8").splitlines()

        todos = []
        pattern = re.compile(r"^\s*- \[([ xX])\] (.+)$")

        for index, line in enumerate(lines):
            match = pattern.match(line)
            if not match:
                continue

            todos.append(
                Todo(
                    line_number=index,
                    description=match.group(2),
                    completed=match.group(1).lower() == "x",
                )
            )

        return todos

    def get_next_todo(self) -> Todo | None:
        for todo in self.get_todos():
            if not todo.completed:
                return todo
        return None

    def complete(self, todo: Todo) -> None:
        lines = self.path.read_text(encoding="utf-8").splitlines()

        current = lines[todo.line_number]
        expected = f"- [ ] {todo.description}"

        if current.strip() != expected:
            raise RuntimeError("TODO.md가 변경되어 완료 처리할 수 없습니다.")

        lines[todo.line_number] = current.replace(
            "- [ ]", "- [x]", 1
        )

        self.path.write_text(
            "\n".join(lines) + "\n",
            encoding="utf-8",
        )
