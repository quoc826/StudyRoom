# Shop balance

Daily quests award 20 coins for visiting, 35 for studying 30 minutes,
30 for being online 60 minutes, 60 for studying 60 minutes, and 55 for
being online 120 minutes. Total: 200 coins. Passive income remains
5 coins per 15 minutes online (40 coins in two hours).

Starting from zero, claiming eligible quests gives:

| Session | Total coins |
| --- | ---: |
| Daily visit | 20 |
| 30 minutes online and studying | 65 |
| 60 minutes online and studying | 165 |
| 120 minutes online, including at least 60 minutes studying | 240 |
| 120 minutes online without studying | 145 |

Small items cost 8–55 coins. Tables cost 80–92, seating 65–102,
storage 70–143, beds 140–152. Prices vary by category, functional
subtype and a stable item-ID variation. Existing purchases are retained.

| Pet | Coins |
| --- | ---: |
| Sparrow | 240 |
| Herring | 270 |
| Gecko | 300 |
| Muskrat | 330 |
| Squid | 360 |
| Taipan | 390 |
| Pudu | 420 |
| Colobus | 450 |

A daily two-hour session with all quests funds the cheapest pet in
one day, or any other pet in two days, assuming no other purchases.
Room unlock prices are unchanged.

`StudyRoomShopPricing` is shared by the catalog builders. Run
`StudyRoom > Balance Shop Prices` to apply it to the existing catalog.
Reward totals were checked through `StudyRoomProgress` using a temporary
save key, including rejection of repeated quest claims; the key was removed.
