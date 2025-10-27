using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AppRestarter
{
    public static class GroupManager
    {
        public static List<string> LoadGroups(XElement root)
        {
            var groups = new List<string>();
            var groupsEl = root.Element("Groups");
            if (groupsEl != null)
            {
                groups = groupsEl.Elements("Group")
                                 .Select(g => (string)g.Attribute("Name"))
                                 .Where(n => !string.IsNullOrWhiteSpace(n))
                                 .Distinct()
                                 .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                                 .ToList();
            }
            return groups;
        }

        public static void SaveGroups(XElement root, IEnumerable<string> groups)
        {
            var groupsEl = root.Element("Groups");
            if (groupsEl == null)
            {
                groupsEl = new XElement("Groups");
                root.AddFirst(groupsEl);
            }
            groupsEl.RemoveNodes();
            foreach (var name in groups.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(name))
                    groupsEl.Add(new XElement("Group", new XAttribute("Name", name)));
            }
        }

        public static void AddGroup(XElement root, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            var groups = LoadGroups(root);
            if (!groups.Contains(groupName, StringComparer.OrdinalIgnoreCase))
            {
                groups.Add(groupName);
                SaveGroups(root, groups);
            }
        }

        public static void RemoveGroup(XElement root, string groupName, bool clearAppsGroup = false)
        {
            var groups = LoadGroups(root);
            var newList = groups.Where(g => !g.Equals(groupName, StringComparison.OrdinalIgnoreCase)).ToList();
            SaveGroups(root, newList);

            if (clearAppsGroup)
            {
                var appsEl = root.Element("Applications");
                if (appsEl != null)
                {
                    foreach (var app in appsEl.Elements("Application"))
                    {
                        var g = (string)app.Element("GroupName");
                        if (!string.IsNullOrEmpty(g) && g.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                        {
                            app.Element("GroupName")?.Remove();
                        }
                    }
                }
            }
        }

        public static void RenameGroup(XElement root, string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return;

            // Update groups list
            var groups = LoadGroups(root);
            if (groups.RemoveAll(g => g.Equals(oldName, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                if (!groups.Contains(newName, StringComparer.OrdinalIgnoreCase))
                    groups.Add(newName);
                SaveGroups(root, groups);
            }

            // Update apps using that group
            var appsEl = root.Element("Applications");
            if (appsEl != null)
            {
                foreach (var app in appsEl.Elements("Application"))
                {
                    var el = app.Element("GroupName");
                    if (el != null && el.Value.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                    {
                        el.Value = newName;
                    }
                }
            }
        }
    }
}
