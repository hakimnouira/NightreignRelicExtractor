# AI Build Optimizer Prompt

Use this prompt with your preferred AI (such as **ChatGPT Plus / Team**, **Claude 3.5 Sonnet**, or **Google Gemini Advanced**) to analyze your extracted `relics.xlsx` or `relics.csv` inventory and generate optimal character builds.

---

## Instructions

1. Run **`NightreignRelicExtractor.exe`** to generate your `relics.xlsx` (or `relics.csv`).
2. Click **📋 Copy AI Build Prompt** in the app (or copy the text below).
3. Open **ChatGPT**, **Claude**, or **Gemini**.
4. **Attach your `relics.xlsx`** (or `relics.csv`) file to the chat.
5. Paste the prompt below and press Enter!

---

## The Prompt

```markdown
I have attached my extracted Elden Ring Nightreign relic inventory (relics.xlsx / relics.csv).

Please act as an expert Elden Ring Nightreign build strategist. Analyze my exact relic inventory and create the best possible builds for my characters.

### Game & Relic Rules:
1. **Relic Capacity**: Each vessel equips up to 6 relics total:
   - 3 Standard Relic slots (fit normal Relics and UniqueRelics).
   - 3 Deep Relic slots (fit DeepRelics).
2. **Slot Color Constraints**: Each vessel slot has a strict color requirement (Red, Blue, Yellow, Green). Relics must match their slot's color.
3. **Character-Specific Perks**: Effects starting with character names (e.g. Wylder, Guardian, Duchess, Raider, Recluse, Scholar, Revenant, Executor) only activate when playing that specific character.
4. **Stacking**: Stackable stat buffs and resistances combine, but effects marked 'Only 1 active (Left priority)' do not stack with duplicates.
5. **No Hallucinated Relics**: Only recommend relics that actually exist in my attached inventory (match by Relic ID and Relic Name).

### What I Want From You:
1. **Top Character Builds**: Recommend the optimal 6-relic combination for:
   - **Wylder** (Physical / Stagger / Skill spam)
   - **Guardian** (Tank / Guard Counter / HP Regen)
   - **Duchess** (Critical / Dagger / Sorcery)
   - **Recluse** (Status effects / Blood loss / High DPS)
   - Any other character you find strong synergies for in my inventory.
2. **Build Breakdown For Each**:
   - Chosen Vessel & Slot Colors
   - List the 6 specific Relics (with their Relic ID, Name, Color, and active Effects)
   - Synergy explanation & gameplay strategy
3. **Inventory Advice**:
   - Highlight the top 5 strongest 'god-roll' relics in my collection.
   - Point out any useless duplicates that can safely be recycled or ignored.

Please review the attached spreadsheet and generate my builds!
```
