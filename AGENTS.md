# StatLab

## What this is

StatLab is a portable C# rules-and-simulation library for a turn-based, co-op RPG, plus a command-line
runner for balance simulations. It's one part of a larger game inspired by WoW and Baldur's Gate.
The game will later reference `StatLab.Core` directly, so the simulator and the game run identical code.

## Working mode: learning

The developer is building this by hand to learn it. By default:

- Explain concepts, review code, suggest test cases, and answer questions.
- Do not write or edit implementation code in `src/` unless explicitly asked.
- When asked for code, keep it minimal and explain the reasoning.
- Point out bugs and drift from the rules below; don't silently fix them.

## Rules

1. `StatLab.Core` has no file IO, no console output, no platform or engine dependencies.
2. Project references point inward only: `Data` → `Core`; `Sim` → `Core`, `Data`. Nothing references `Sim`.
3. Determinism: all randomness goes through the simulation's seeded `DeterministicRng`. Same seed + same commands = same result. No `System.Random`, clock time, or static mutable state in `Core`.
4. Formulas live in code. Tuning numbers live in `data/`.
5. Rulesets (`IRuleset`) are swapped whole, never formula by formula. Each ruleset has its own data folder.
6. Calculate in floating point; round only at final output.
7. The modifier pipeline order is fixed. Changing it requires updating its spec and tests together.
8. `Core` emits events; it never renders, prints or logs.
9. Look up stats by `StatId` (array index), not by string, in hot paths.

## Where to go

Read `CONTEXT.md` to find the files for the current task. Load only what the task needs.

## Commands

```
dotnet build
dotnet test
```