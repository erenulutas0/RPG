"""The balance rule's read (docs/56): a paired, seed-level bootstrap of four-floor completion changes.

usage: python Tools/BalanceRule/paired_bootstrap.py <folder> [first-seed last-seed]
<folder> holds GloamEnemiesResearch outputs, one run each in a subfolder (FOUNDRY_OUT=<folder>/<name>), every one with
`shipped` or paired against a `shipped` run of the same seeds elsewhere in <folder>. Typical hands only. For each
comparison below and each cohort it prints, over the seeds asked (1-800 by default): the change in completions (two
heroes a seed), in completion-rate points, the bootstrap 90 % CI (10,000 resamples of seeds, both heroes of a seed
together, a fixed generator seed so the table repeats), the SD of the paired change per 400 seeds, and the change on
each half of the seeds. A comparison with flip reports shipped minus the option: the shipped creature against what it
replaced. A comparison with a base other than `shipped` reads the option against that base's runs of the same seeds
(the Glass Regent's round of 2026-11-17 reads its candidates against `f2-warden`, the Forge Warden back at floor 2's
gate). A comparison whose option or base was not run on every seed asked is skipped.
"""
import csv
import glob
import os
import random
import sys
from collections import defaultdict

root = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
runs = {}
for path in glob.glob(os.path.join(root, '*', 'gloam-enemies.csv')):
    with open(path, newline='') as f:
        for r in csv.DictReader(f):
            if r['realism'] != 'typical human':
                continue
            runs[(r['option'], r['cohort'], r['hero'], int(r['seed']))] = r['finale_outcome'] == 'WalkedOut'

COMPARISONS = [
    # (label, option, flip[, base]): the base is `shipped` unless named
    ('A/A: one unread draw from floor 2 (aa-f2)', 'aa-f2', False),
    ('A/A: one unread draw from floor 3 (aa-f3)', 'aa-f3', False),
    ('A/A: two unread draws from floor 3 (aa-f3b)', 'aa-f3b', False),
    ('A/A: three unread draws from floor 3 (aa-f3c)', 'aa-f3c', False),
    ('A/A: two unread draws from floor 2 (aa-f2b)', 'aa-f2b', False),
    ('A/A: three unread draws from floor 2 (aa-f2c)', 'aa-f2c', False),
    ('Mirror Sentry vs the Grunt Captain (floor 2 elite, shipped)', 'f2-captain', True),
    ('Glass Regent vs the Forge Warden (floor 2 gate, shipped)', 'f2-warden', True),
    ('Cinder Bloom vs the Grunt Captain (floor 3 elite, shipped)', 'captain', True),
    ('Thornling `thorn` (0.8 s)', 'thorn', False),
    ('Thornling `thorn-t1.2`', 'thorn-t1.2', False),
    ('Thornling `thorn-dv3-6`', 'thorn-dv3-6', False),
    ('Thornling `thorn-t1.0`', 'thorn-t1.0', False),
    ('Thornling, no damage, nobody dodges (`thorn-s0-unseen`)', 'thorn-s0-unseen', False),
    # The Glass Regent's round of 2026-11-17 (docs/50 "The Regent's bite for the standing hero"): its candidate against the
    # Forge Warden (the read the lead's decision asks for) and against the Regent before the round (`shipped` then).
    ('Regent round: the rod at its feet (`regent-feet`) vs the Forge Warden', 'regent-feet', False, 'f2-warden'),
    ('Regent round: the rod at its feet (`regent-feet`) vs the Regent before it', 'regent-feet', False),
]
COHORTS = ['clumsy', 'careful', 'first-card', 'standing']
HEROES = ['Vanguard', 'Ilya']
SEEDS = range(int(sys.argv[2]), int(sys.argv[3]) + 1) if len(sys.argv) > 3 else range(1, 801)
B = 10000


def seed_changes(option, cohort, flip, base='shipped'):
    out = []
    for s in SEEDS:
        d = 0
        for h in HEROES:
            a = runs.get((option, cohort, h, s))
            b = runs.get((base, cohort, h, s))
            if a is None or b is None:
                return None
            d += (b - a) if flip else (a - b)
        out.append(d)
    return out


def summary(d):
    n = len(d)
    total = sum(d)
    mean = total / n
    var = sum((x - mean) ** 2 for x in d) / (n - 1)
    sd400 = (var * 400) ** .5
    rng = random.Random(56)  # one fixed generator a comparison: a table repeats, and adding a row moves no other
    boots = []
    for _ in range(B):
        boots.append(sum(d[rng.randrange(n)] for _ in range(n)))
    boots.sort()
    lo, hi = boots[int(.05 * B)], boots[int(.95 * B) - 1]
    runs_n = 2 * n
    pts = lambda x: 100.0 * x / runs_n
    return total, pts(total), pts(lo), pts(hi), sd400, sum(d[:len(d) // 2]), sum(d[len(d) // 2:])


print(f'Seeds {SEEDS.start}-{SEEDS.stop - 1}, both heroes ({2 * len(SEEDS)} runs a cohort).')
print('| Comparison | Cohort | Change | Points | 90 % CI (points) | SD per 400 seeds (completions) | First half / second half |')
print('|---|---|---|---|---|---|---|')
for label, option, flip, *base in COMPARISONS:
    for cohort in COHORTS:
        d = seed_changes(option, cohort, flip, *base)
        if d is None:
            continue
        total, p, lo, hi, sd400, a, b = summary(d)
        print(f'| {label} | {cohort} | {total:+d} | {p:+.2f} | {lo:+.2f} to {hi:+.2f} | {sd400:.1f} | {a:+d} / {b:+d} |')
