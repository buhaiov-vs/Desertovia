# Full sprite generation progress

Goal: generate the entire list in docs/sprites.md, including separate construction stages and character animation variants. Preserve rows explicitly assigned to code or official brand assets. No game integration has been performed.

## Generated so far

- All 18 resource images (samples, resources-02, resources-03).
- All 22 world building images (samples, buildings-01, buildings-02).
- All 22 building icons (building-icons), exported at 16x16 with transparent corners verified.
- Citizen, male style 1 (samples).

216 unique assets generated, including 62 construction stages, four section-5 construction props/terrain, all 26 farming drafts and all 24 village project drafts (six icons and eighteen pieces/buildings). Each has a source PNG, a game-size export and an enlarged preview. The catalog tracks generated outputs separately from final style approval. It currently has 401 document rows; construction stages and animation rows require expansion into multiple deliverables, so that is not a final image count.

## Next work

All 62 section-4 construction-stage drafts and four required section-5 images are generated; placement outline remains code-drawn as directed. Six section-6 farming bed drafts are generated. All five crops have growth stages and harvested icons generated. Section 6 has all 26 draft outputs; final style, alignment and terrain-seam audits remain open. Section 7 has all six project icon drafts. All 18 project pieces/buildings have drafts, including Great Granary (64x48) and Bell Tower (48x64). Section 7 generation pass is complete, with corrections still outstanding. Section 8 now has seven terrain drafts: three grass, stony ground, river water and both river banks. Pond water and all sixteen pond shore masks (0–15) now have drafts; pond-catalog.json tracks all combinations. Mountain rock and three bare mountain peaks now have drafts. All three mossy peak variants also have drafts. Trampled earth and both birches have drafts. Tall and young spruce plus oak now have drafts; all five tree variants are generated. Next: tree stump, sapling, berry bush and remaining section 8 scenery, then sections 9–21. construction-catalog.json tracks all 62 deliverables independently; original section-4 rows stay pending until audit. Expand animation rows into their required separate deliverables. The buildings-02 exporter supports rectangular canvases and skips existing exports. All 17 new building exports passed exact dimension checks.

## Final audit remains

Review generated silhouettes at native size, simplify excess shading to match the Kenney references, enforce character recolor regions, verify terrain edges and frame alignment. In resources-03 the chain is too diagonal compared with the requested S curve; grain has a stray red edge pixel, and scroll/gear details are weak at native size. Buildings-02 drying rack has an unwanted halo; an image-tool edit (v2-source) failed to remove it and was not selected. Other new buildings have occasional colored edge pixels and more shading than the reference palette. No generated status claims these style requirements have passed.

Generation uses built-in image_gen, one call per distinct asset. Exact prompts are in each batch's prompts.json. Sources were copied from the image tool's returned paths. No CLI image generator or external API key was used.



Construction-01 uses full-canvas nearest-neighbor exports to preserve empty space. Generated stage geometry still drifts from finished references, especially lean-to supports; footprint/alignment and palette audit remains mandatory. Heap has excess perimeter framing; wood pile retains an axe and stump from its reference.


Construction-02: both drying-rack stages inherited a strong unwanted halo and require regeneration from clean references. Hut stages have structural drift and stray edge pixels; roof is more than half thatched. All eight exports preserve full canvas, but alignment has not passed audit.


Construction-03: eight drafts exported. Stone-yard ground was corrected in a separate v2 source to remove unwanted masonry; exporter selects v2 and preserves original. Woodyard ground/foundation footprints differ; stone-yard wall height, geometry across frames and colored edge pixels require audit/correction.


Construction-04: five granary drafts. Support locations drift between stages, ground string has edge artifacts, and roof stage does not retain completed log walls. Those need correction before final acceptance.


Construction-05: five Storehouse drafts. Foundation thickness, frame depth and base positions drift between stages. Ground string has colored fringe pixels. Final alignment and palette audit remains open.


Construction-06: three Farm drafts. Field v2 removes unwanted fencing; original retained. Ground and field align more closely; partial fence footprint and furrow spacing still differ. All need palette/edge cleanup and final alignment audit.


Construction-07: four Bakery drafts. Oven is unlit and roof is half thatched; foundation thickness, footprint and oven proportions drift across stages. Ground string and roof have edge artifacts; palette/alignment audit remains open.


Construction-08: three Mine drafts with distinct marked rock, excavated hole and timber supports. Rock silhouettes are similar but require exact alignment audit; palette simplification, chalk visibility and edge artifacts remain to review.


Construction-09: four Smithy drafts. Roof stage loses chimney stub and adds a gable detail; must correct. Foundation/forge geometry and base positions vary, with colored fringe pixels. Final palette/alignment audit remains open.


Construction-10: four Workshop drafts. Ground has a severe haze/halo requiring regeneration. Frame and roof supports differ, foundation geometry shifts; palette and edge cleanup remain.


Construction-11: five Library drafts. Tall frame and steep half-tiled roof present; foundation, post arrangement and heights drift between stages. Ground string has edge fringe; simplification and alignment remain open.


Construction-12: five Tavern drafts. Wall-stage masonry is too tall, foundations/post positions change across stages, and ground string has fringe artifacts. Roof draft shows completed log walls and half thatch. Final alignment/palette correction remains.

Construction-13: three Stage drafts. Ground string has colored edge artifacts; platform depth, supports and base positions drift between floor and frame. Palette simplification and exact alignment remain open.

Construction-props: four 16x16 exports verified. Peg is too small within its canvas; signboard hammer is weak; ground tile loses recognizable footprints/chips and requires simplification plus seamless-edge validation; pallet needs palette/edge cleanup. All remain drafts awaiting final audit.


Farming-beds: six 16x16 exports verified. Neighboring grass colors and furrow geometry mismatch; wet glints disappear at native size. Flat palette and seamless joins require correction. All six remain drafts.


Farming-turnip: four 16x16 transparent exports verified. Distinct growth silhouettes; neon-green fringe on sprout, excess shading, plant anchors and exposed ripe bulb need final correction/review.


Farming-carrot: four 16x16 transparent exports verified. Young foliage is oversized; sprout shape, plant anchors, bright edges and excess shades require correction. Harvested carrot and ripe shoulder remain readable at native size.


Farming-cabbage: four 16x16 transparent exports verified. Sprout leaves merge at native size; ripe/harvested folded-leaf shapes, gradients, palette, edge artifacts and plant-base alignment require final correction/review.


Farming-flax: four 16x16 transparent exports verified. Severe bright green/white sprout artifacts, inconsistent stem colors, broken thin stems and thick harvested stalks need repair; five-petal flowers lose detail at native size. Palette and alignment audit remain open.


Farming-rye: four 16x16 transparent exports verified. Sprout greens are neon, thin stalks break, ripe ears/whiskers lose definition with a red artifact, and sheaf binding needs clarity. Palette/edge/anchor audit remains open.


Project-icons-01: three 16x16 transparent exports verified. Monument crown/sceptre need stone coloring and clearer silhouette; fishing rod and palisade rope are weak at native size. Palette/edge audit remains open.


Project-icons-02: three 16x16 transparent exports verified. Bridge has extra bank detail, granary sacks merge and roof touches edge, tower is too narrow for bell/dome clarity. Palette and silhouette corrections remain open.


Project-monument: three 16x16 transparent exports verified. Pedestal undersized with weak/missing shadow, rough block too sculpted/narrow, statue details weak. All-grey material achieved; stacking alignment and palette/edge audit remain open.


Project-pier: two 16x16 transparent exports verified. Ripple rings collapse into blocks; deck source aspect differs from requested square and board divisions are weak. Six of sixteen deck edge pairs differ. Seam, proportions, palette and assembly alignment require correction.


Project-palisade-01: three 16x16 exports verified. Vertical geometry is not a repeating north-south wall; horizontal has severe halo (v2 repair unsuccessful, unselected) and inset ends. NW corner has sideways tips and scale mismatch. Geometry/transparency/joins/palette need correction.


Project-palisade-02: four 16x16 exports verified. SW corner/end scale differs from straight sections; both gate-post wall directions are wrong and require regeneration. Joins, geometry, palette and edge corrections remain open.


Project-bridge: four 16x16 exports verified. Deck edges inset; rail height/supports/end-post shapes vary, left adds bottom beam, right misses left edge and has red artifact. Assembly, seams, palette and edge repairs remain open.


Project-buildings: Great Granary 64x48 and Bell Tower 48x64 exports verified, proportional nearest-neighbor fit with margin. Required major features present. Excess shading/detail, edge artifacts, tower perspective and footprint audit remain open.


Scenery-grass: three opaque 16x16 exports generated. Palette/grid and seam audits failed: 34/36/48 colors and opposite-edge mismatches recorded in batch README. Tiny details lose shape at native size; correction required before integration.


Scenery-river: four opaque 16x16 exports generated. Bank directions are correct; palette/grid and required seams fail. Detailed edge counts in batch README. Foam loses continuity at native resolution. Optional river animation not generated.


Scenery-pond: water and shore masks 0/1/2 generated, all 16x16 opaque exports. Correct assigned corner materials, but rim width and midpoint alignment drift. Lily notch disappears at native size; palette and seam checks remain open. See batch README. Full 16-mask prompt set persisted; 13 shore tiles remain pending.


Pond masks 3–6: four drafts generated and verified 16x16 with opaque corners. Mask 5 preserves separated diagonal water. Mask 6 initial source had an erroneous horizontal shore; v2 removes it and is selected by exporter, with original preserved. All still need palette and seam alignment corrections; mask 3 transitions far from edge midpoints.


Pond masks 7–10: four 16x16 drafts exported with opaque corners. Mask 7 has sand in NW/SE instead of required water; mask 9 has an erroneous sandy branch crossing the water. Masks 8 and 10 have correct corner placement; mask 10 keeps diagonal waters disconnected. All require seam/palette correction.


Pond masks 11–15: five drafts exported, completing all sixteen mask drafts. Dimensions and opaque corners verified. Mask 11 incorrectly caps the SW water corner with shore/grass; masks 12–14 corner materials are correct but transition positions drift. Mask 15 ripple loses shape at native size. Set is generated, not seam-approved; original Pond shore document row remains pending acceptance.


Mountains: four drafts exported, dimensions and top-left alpha verified. Three silhouettes distinct; peak 1 stray red pixel, excessive shading across peaks, terrain too busy. Ridge alignment and terrain seams remain open. Mountain rock and Mountain peak rows were missing from catalog.json and have been restored from docs/sprites.md; document coverage requires reconciliation before final completion.


Mossy peaks: all three generated as edits of bare peak sources, 32x32 exports with transparent top-left corners. Alpha-threshold silhouette differences against bare variants: 2, 1, 0 pixels respectively. Peak 1 retains stray red pixel; excess shading and subdued moss readability require cleanup. Six peak variant drafts now tracked in scenery-variants-catalog.json.


Scenery-birch: three exports verified dimensions/top-left alpha. Earth v2 removes unwanted fire ring and is selected. Trees have grassy bases, excess shading and edge artifacts; tall crown too narrow, young sprite small in canvas. Terrain seams and all style audits remain open.


Scenery-trees: three sprites exported with aspect-preserving bounds fit and one-pixel margin. Dimensions/top-left alpha verified. Native previews show readable spruce and oak silhouettes; palette gradients, edge fringes and relative trunk/crown scale require final cleanup.

