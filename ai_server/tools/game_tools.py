"""FPS game tool definitions in OpenAI function-calling format.
These are sent to Ollama so the LLM can invoke game actions.
"""

GAME_TOOLS: list[dict] = [
    {
        "type": "function",
        "function": {
            "name": "execute_command",
            "description": "Execute an in-game FPS command such as reloading, switching weapons, or using items.",
            "parameters": {
                "type": "object",
                "properties": {
                    "action": {
                        "type": "string",
                        "description": "The game action to execute.",
                        "enum": [
                            "reload",
                            "switch_weapon",
                            "use_item",
                            "throw_grenade",
                            "melee_attack",
                            "interact",
                            "open_map",
                            "open_inventory",
                        ],
                    },
                    "target": {
                        "type": "string",
                        "description": "Optional target or parameter for the action.",
                    },
                },
                "required": ["action"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "npc_emote",
            "description": "Make an NPC display an emotion or play an animation.",
            "parameters": {
                "type": "object",
                "properties": {
                    "emotion": {
                        "type": "string",
                        "description": "The emotion or animation to play.",
                        "enum": [
                            "happy",
                            "angry",
                            "sad",
                            "surprised",
                            "neutral",
                            "thinking",
                            "salute",
                        ],
                    },
                },
                "required": ["emotion"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "give_hint",
            "description": "Give the player a gameplay hint about objectives, enemies, or items.",
            "parameters": {
                "type": "object",
                "properties": {
                    "hint_level": {
                        "type": "string",
                        "enum": ["subtle", "moderate", "direct"],
                        "description": "How explicit the hint should be.",
                    },
                    "target_object": {
                        "type": "string",
                        "description": "The object or objective the hint is about.",
                    },
                },
                "required": ["hint_level"],
            },
        },
    },
]
