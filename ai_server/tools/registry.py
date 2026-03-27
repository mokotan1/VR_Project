from __future__ import annotations

from copy import deepcopy


class ToolRegistry:
    """Central registry for AI function-calling tool definitions (OpenAI format).
    Ported from newCapstone backend_ai/tools/registry.py.
    """

    def __init__(self) -> None:
        self._tools: dict[str, dict] = {}

    def register(self, tool_definition: dict) -> None:
        name = tool_definition["function"]["name"]
        self._tools[name] = deepcopy(tool_definition)

    def register_many(self, definitions: list[dict]) -> None:
        for defn in definitions:
            self.register(defn)

    def get_tool(self, name: str) -> dict | None:
        return deepcopy(self._tools.get(name))

    def get_all_openai_format(self) -> list[dict]:
        return [deepcopy(t) for t in self._tools.values()]

    @property
    def tool_names(self) -> list[str]:
        return list(self._tools.keys())

    def __len__(self) -> int:
        return len(self._tools)
