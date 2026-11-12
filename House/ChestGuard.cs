using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TShockAPI;

namespace TerraTopia.House
{
    public class ChestGuard
    {
        public void Register()
        {
            try
            {
                foreach (var t in typeof(TShock).Assembly.GetTypes())
                {
                    if (t.Namespace == null || !t.Namespace.StartsWith("TShockAPI"))
                    {
                        continue;
                    }

                    if (!t.Name.Contains("Hook"))
                    {
                        continue;
                    }

                    var members = new List<string>();
                    foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        if (m.Name.Contains("Chest"))
                        {
                            members.Add(m.MemberType + ":" + m.Name);
                        }
                    }

                    if (members.Count > 0)
                    {
                        TShock.Log.ConsoleInfo($"[ChestGuard] {t.FullName} -> {string.Join(", ", members)}");
                    }
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error("[TerraTopia] ChestGuard diagnostik gagal: " + ex.Message);
            }
        }

        public void Unregister() { }
    }
}
