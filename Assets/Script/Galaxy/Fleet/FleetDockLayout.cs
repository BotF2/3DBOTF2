using UnityEngine;

namespace BOTF3D.Galaxy
{
    /// <summary>
    /// Non-overlapping dock offsets for fleets sitting at a star system, arranged in an outward
    /// square spiral around the system so newly produced fleets don't render stacked on top of
    /// each other. Slot 0 (Fleet 1's spot) matches the original single fixed offset used before
    /// this system existed.
    /// </summary>
    public static class FleetDockLayout
    {
        private const float SlotSpacing = 20f;
        private static readonly Vector3 BaseOffset = new Vector3(0f, 20f, 10f);

        public static Vector3 GetSlotOffset(int slotIndex)
        {
            if (slotIndex < 0) slotIndex = 0;
            Vector2Int grid = GetSpiralGridPoint(slotIndex);
            return BaseOffset + new Vector3(grid.x * SlotSpacing, grid.y * SlotSpacing, 0f);
        }

        // Same square-spiral walk as ShipFormationManager.GenerateSpiralPositions (combat wall
        // formation), just computed for a single index instead of a whole list up front, since dock
        // slots are claimed/released one at a time as fleets arrive and depart.
        private static Vector2Int GetSpiralGridPoint(int index)
        {
            if (index == 0) return Vector2Int.zero;

            int x = 0, y = 0;
            int dx = 0, dy = -1;

            for (int i = 1; i <= index; i++)
            {
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }
                x += dx;
                y += dy;
            }

            return new Vector2Int(x, y);
        }
    }
}
