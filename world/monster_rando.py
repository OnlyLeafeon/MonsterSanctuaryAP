
def fnv1a(seed: str) -> int:
    """FNV-1a hash over UTF-16 code units, returned as a signed 32-bit int."""
    h = 2166136261
    for ch in seed:
        h ^= ord(ch)
        h = (h * 16777619) & 0xFFFFFFFF
    return h - 0x100000000 if h >= 0x80000000 else h


def _to_int32(x: int) -> int:
    """Emulate C#'s 32-bit signed int wraparound, which Python's
    arbitrary-precision ints don't do on their own."""
    x &= 0xFFFFFFFF
    if x >= 0x80000000:
        x -= 0x100000000
    return x


class DotNetRandom:
    """Bit-compatible clone of the .NET Framework System.Random used by the client."""

    MBIG = 2147483647  # int.MaxValue
    MSEED = 161803398

    def __init__(self, seed: int):
        _seed = 2147483647 if seed == -2147483648 else abs(seed)
        num = self.MSEED - _seed

        self._sa = [0] * 56
        self._sa[55] = num

        num3 = 1
        for i in range(1, 55):
            index = (21 * i) % 55
            self._sa[index] = num3
            num3 = _to_int32(num - num3)
            if num3 < 0:
                num3 += self.MBIG
            num = self._sa[index]

        for _j in range(1, 5):
            for k in range(1, 56):
                self._sa[k] = _to_int32(self._sa[k] - self._sa[1 + (k + 30) % 55])
                if self._sa[k] < 0:
                    self._sa[k] += self.MBIG

        self._inext = 0
        self._inextp = 21

    def _sample(self) -> float:
        inext = self._inext + 1
        if inext >= 56:
            inext = 1
        inextp = self._inextp + 1
        if inextp >= 56:
            inextp = 1
        num = _to_int32(self._sa[inext] - self._sa[inextp])
        if num == self.MBIG:
            num -= 1
        if num < 0:
            num += self.MBIG
        self._sa[inext] = num
        self._inext = inext
        self._inextp = inextp
        return num * 4.6566128752457969e-10

    def next_int(self, min_value: int, max_value: int) -> int:
        """Replica of Next(minValue, maxValue) for ranges <= int.MaxValue."""
        return int(self._sample() * (max_value - min_value)) + min_value

    def _sample(self) -> float:
        inext = self._inext + 1
        if inext >= 56:
            inext = 1
        inextp = self._inextp + 1
        if inextp >= 56:
            inextp = 1
        num = self._sa[inext] - self._sa[inextp]
        if num == self.MBIG:
            num -= 1
        if num < 0:
            num += self.MBIG
        self._sa[inext] = num
        self._inext = inext
        self._inextp = inextp
        # Same double constant as System.Random.Sample().
        return num * 4.6566128752457969e-10

    def next_int(self, min_value: int, max_value: int) -> int:
        """Replica of Next(minValue, maxValue) for ranges <= int.MaxValue."""
        return int(self._sample() * (max_value - min_value)) + min_value



def sort_key(name: str, monster_id: int):
    """Client ordering: name (ordinal) first, then Referenceable.ID.

    For the monster names used in the game this matches Python's string sort,
    which compares by Unicode code point (== UTF-16 code units for BMP text).
    """
    return (name, monster_id)


def shuffle(monsters, seed: str):
    """Fisher-Yates shuffle replicating the client's seeded shuffle.

    `monsters` must already be in client order (see `sort_key`). Returns a new
    list in shuffled order.
    """
    out = list(monsters)
    seed_hash = fnv1a(seed)
    if seed_hash == 0 or len(out) < 2:
        # Client bails out when the seed hash is 0, leaving the default order.
        return out
    rng = DotNetRandom(seed_hash)
    for i in range(len(out) - 1, 0, -1):
        k = rng.next_int(0, i + 1)
        out[i], out[k] = out[k], out[i]
    return out


def referenceable_id(item):
    """Return (name, id) from a monster entry.

    Accepts either a (name, id) tuple/list or a simple name string (id = None).
    """
    if isinstance(item, (tuple, list)):
        name = item[0]
        monster_id = item[1] if len(item) > 1 else None
        return name, monster_id
    return item, None


def randomize_monsters(seed: str, monsters):
    """Compute the client's fixed global monster replacement mapping.

    Args:
        seed:     the Archipelago room seed string (same value the client hashes).
        monsters: an iterable of monster entries. The client pool is every
                  Referenceable prefab with a Monster component. Each entry can
                  be a plain name string or a (name, Referenceable.ID) pair.

    Returns:
        A dict mapping each input monster entry (as passed) to its replacement
        entry (name, id) taken from the shuffled pool. source -> replacement.
    """
    entries = []
    for item in monsters:
        name, monster_id = referenceable_id(item)
        entries.append((name, monster_id if monster_id is not None else 0))

    ordered = sorted(entries, key=lambda e: sort_key(e[0], e[1]))
    shuffled = shuffle(ordered, seed)

    result = {}
    for i, (name, monster_id) in enumerate(ordered):
        source = (name, monster_id) if monster_id else name
        r_name, r_id = shuffled[i]
        replacement = (r_name, r_id) if monster_id else r_name
        result[source] = replacement
    return result


def replacement_at(seed: str, monsters, index: int):
    """Replicate MonsterRandomizer.GetReplacementAt(index) for starters.

    Returns the monster occupying `shuffled[index]`, or None when the index is
    out of range. Note the starter patch prefers GetReplacementMonster(familiar)
    and only falls back to this when the familiar prefab is not in the pool.
    """
    entries = [referenceable_id(e) for e in monsters]
    ordered = sorted(entries, key=lambda e: sort_key(e[0], e[1] if e[1] is not None else 0))
    if index < 0 or index >= len(ordered):
        return None
    return shuffle(ordered, seed)[index]


if __name__ == "__main__":
    demo_pool = [f"Monster{i}" for i in range(12)]
    print("seed hash test:", fnv1a("test seed!"))  # expected: 66490153
    perm = shuffle(demo_pool, "test seed!")
    print("permutation:", [int(n[7:]) for n in perm])  # expected: 11,8,4,5,7,1,3,0,10,2,6,9