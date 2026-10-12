# Desertod — Roadmap

Status as of 2026-10-12. Design documents: [game-design.md](game-design.md), [streamer-mode.md](streamer-mode.md), [cooperative-building.md](cooperative-building.md).

## How to use this file

- Tick an item (`- [x]`) when it is merged and works, not when it is started.
- Add a short note after the item if it was done differently from the plan, and update the design document it belongs to.
- New work goes under the phase it belongs to. If something blocks launch, add it to Phase 0.
- Update the date at the top when you change this file.

## Done

- Game rules (`Desertod.Domain`): stats, roles, experience curve, energy, challenge multiplier, streak, morale, hunger, shelter, expeditions and rare finds, storage buildings with slots, carried goods. Covered by domain tests.
- Cooperative building: blueprints placed anywhere on free camp land, sequential stages with parallel haul and labor jobs, crew requirement with "Lend a hand", repeat cost × 1.15, walking time by distance.
- Wild start: campfire and Heap, stats at 0, name-only character creation, seven starting tasks.
- Buildable buildings: Heap, Lean-to, Wood pile, Stone pile, Drying rack, Hut, Woodyard, Stone yard, Granary.
- Tribe needs (quests): hunger, cold, storage room; Renown split among contributors.
- Discoveries (technologies): 16 discoveries in Camp, Village and Town eras; the Governor calls a vote and citizens choose, qualified citizens research at the discovery's building with a crew requirement; discoveries unlock Farm, Bakery, Workshop, Mine, Smithy, Library, Tavern, Stage and Storehouse, roles and yield bonuses. New early tasks: Tell stories, Study plants, Explore the land. See [cooperative-building.md](cooperative-building.md), section 5.
- Wildlife: rabbit, deer and bird herds per village that shrink when hunted and grow back over time; Hunt rabbits, Hunt birds (feathers), Hunt deer (Hunter). See [cooperative-building.md](cooperative-building.md), section 8.
- Farming: 3 beds per Farm; sow turnips, carrots, cabbage, flax or rye (better seeds need more Endurance), water the beds (anyone), harvest; Farmer title. Bakery (Baking discovery): Bakers bake grain into food. See [cooperative-building.md](cooperative-building.md), Farming.
- Backend: ASP.NET Core, PostgreSQL, SignalR live updates, background cycle settlement, concurrency retries, translatable error codes.
- Login: Twitch, Google (YouTube), Telegram, name-only login in Development.
- Frontend: React + PixiJS village view, minimap, task list, build panel, sites, needs, inventory, character sheet, first-steps guide, notifications, stream overlay without login.
- Public and private villages with invite codes, public village list.
- English and Russian, typed dictionaries.
- Server bootstrap: hardened `deploy/cloud-init.yml`.

## Phase 0 — Stabilize (launch blockers)

Goal: a buildable, deployable application.

- [ ] Initialize git and push to a remote repository.
- [ ] Fix the frontend build (`npm run build` in `web`):
  - [ ] `web/src/i18n/ru.ts`: 29 missing Russian keys (`home.foundHint`, `home.open`, `home.noOpen`, `home.citizens`, …).
  - [ ] `web/src/pages/HomePage.tsx`: `api.createVillage` is called with a name; it now expects a `CreateVillageRequest`.
  - [ ] `web/src/ui/CreateCitizenForm.tsx`: the call is missing the invite code argument.
  - [ ] Finish the public/private village user interface (create form, invite code entry).
- [ ] `Dockerfile` for the application (frontend build into `wwwroot`, then the .NET publish).
- [ ] Production `docker-compose` with the application, PostgreSQL and a reverse proxy with automatic HTTPS (for example Caddy).
- [ ] Forwarded headers, so Twitch and Google login build `https://` redirect addresses behind the proxy.
- [ ] Persist data protection keys (database or volume), so login cookies survive restarts.
- [ ] Apply database migrations in production (at startup or as a separate deployment step; today only in Development).
- [ ] Health check endpoint.
- [ ] Register the Twitch, Google and Telegram login applications with the production domain.
- [ ] Nightly database backups stored off the server.
- [ ] Rate limiting on authentication, task start, job start and village creation endpoints, and on hub connections.
- [ ] Privacy policy and terms page.
- [ ] Continuous integration: `dotnet test` and `npm run build` on every push.

## Phase 1 — Closed beta

Goal: one or two friendly streamers playing for real.

- [x] Make Workshop buildable (stages and jobs), so planks exist. Needs the Carpentry discovery.
- [x] Make Smithy buildable, so iron ingots exist. Needs the Smelting discovery.
- [x] Make Farm buildable. Needs the Agriculture discovery.
- [ ] Start a first monument in every village (today new villages never get an active project), with materials adjusted to the wild start.
- [ ] Error tracking and structured log collection.
- [ ] Application programming interface integration tests (start task, settle, build, quests) against a real PostgreSQL.
- [ ] Play-test stream with 5–10 people; record balance findings here.
- [ ] Tune crew sizes, hunger, energy and job amounts from the play-test.

## Phase 2 — Ages

Goal: long-term goals. See [cooperative-building.md](cooperative-building.md), section 5.

- [x] Age system. Done differently: eras come from discoveries that citizens vote for and research at buildings, not from building counts.
- [x] Camp age and Village age buildings with stages.
- [ ] Balance discovery costs and crews from a play-test.
- [ ] Better houses (House, Manor) and house upgrades.
- [ ] Palisade built per wall segment.
- [x] Roles unlocked by their building existing. Done differently: roles are unlocked by their discovery.

## Phase 3 — Streamer tools

Goal: more for the Governor to do on stream. See [streamer-mode.md](streamer-mode.md).

- [ ] Decrees: Festival, Bell call, Tax relief, Call to the site.
- [ ] Construction site priority order.
- [ ] Storage orders (accept, refuse, empty).
- [ ] Cancel a started site with its materials left as a pile.
- [ ] More overlay moments: celebrations, rare finds, arrivals.

## Phase 4 — Deeper cooperation

Goal: "we need more people". See [cooperative-building.md](cooperative-building.md), sections 3 and 4.

- [ ] Job places per job and per workplace.
- [ ] Heavy jobs (need two citizens in overlapping cycles).
- [ ] Group hunting bonus.
- [ ] Arrows made from feathers, used by hunting (feathers are collected but have no use yet).
- [ ] Personal share cap per stage.

## Phase 5 — Subscriber integration

Goal: subscriptions become citizenship. See [game-design.md](game-design.md), section 9.

- [ ] Twitch subscription check per village.
- [ ] YouTube channel membership check (YouTube Data application programming interface).
- [ ] Dormant citizens when a subscription lapses; wake up on resubscribe.
- [ ] Cosmetics, titles and house decorations paid with Renown.

## Phase 6 — Later ages and events

Goal: an endless list of goals.

- [ ] Town and Stronghold ages: Stone yard upgrades, Treasury, Market, Barracks. (Storehouse, Tavern, Stage, Library and Mine are buildable behind discoveries.)
- [ ] Roads and haul distance.
- [ ] Raids and defense: Patrol (Guard) and Command (Captain).
- [ ] Trade at the Market (Merchant).
- [ ] Diplomacy between villages (Envoy, Negotiate).
- [ ] Draft plans (Architect) and Lead a crew (Foreman).
- [ ] Bot detection by renewal timing, with a flag for the streamer.

## Open design questions

From [game-design.md](game-design.md), section 11. There is no single right answer; record the decision here and in the design document.

- [ ] Seasons: do villages reset periodically or persist forever? Decide before public launch, because changing it later means wiping or migrating player data.
- [ ] Endurance as a fifth stat: keep or drop?
- [ ] Respec: can citizens lower one stat to raise another?
- [ ] Injury risk for tasks like deep mining (needs Alchemist remedies)?
