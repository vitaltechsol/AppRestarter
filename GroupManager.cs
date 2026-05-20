using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AppRestarter
{
    public static class GroupManager
    {
        public static List<GroupDetails> LoadGroupDetails(XElement root)
        {
            var groups = new List<GroupDetails>();
            var groupsEl = root.Element("Groups");
            if (groupsEl != null)
            {
                foreach (var g in groupsEl.Elements("Group"))
                {
                    var name = (string)g.Attribute("Name");
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        var dontWarn = bool.TryParse((string)g.Attribute("DontWarn"), out var dw) && dw;
                        groups.Add(new GroupDetails 
                        { 
                            Name = name, 
                            DontWarn = dontWarn 
                        });
                    }
                }
            }
            return groups.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static List<string> LoadGroups(XElement root)
        {
            return LoadGroupDetails(root).Select(g => g.Name).ToList();
        }

        public static void SaveGroupDetails(XElement root, IEnumerable<GroupDetails> groups)
        {
            var groupsEl = root.Element("Groups");
            if (groupsEl == null)
            {
                groupsEl = new XElement("Groups");
                root.AddFirst(groupsEl);
            }
            groupsEl.RemoveNodes();
            foreach (var group in groups.Where(g => !string.IsNullOrWhiteSpace(g.Name))
                                        .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                                        .Select(grp => grp.First()))
            {
                var elem = new XElement("Group", new XAttribute("Name", group.Name));
                if (group.DontWarn)
                    elem.Add(new XAttribute("DontWarn", "true"));
                groupsEl.Add(elem);
            }
        }

        public static void SaveGroups(XElement root, IEnumerable<string> groups)
        {
            var groupDetails = groups.Select(name => new GroupDetails { Name = name }).ToList();
            SaveGroupDetails(root, groupDetails);
        }

        public static void AddGroup(XElement root, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            var groups = LoadGroupDetails(root);
            if (!groups.Any(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
            {
                groups.Add(new GroupDetails { Name = groupName });
                SaveGroupDetails(root, groups);
            }
        }

        public static void UpdateGroup(XElement root, string oldName, GroupDetails updatedGroup)
        {
            if (string.IsNullOrWhiteSpace(oldName) || updatedGroup == null) return;

            var groups = LoadGroupDetails(root);
            var existing = groups.FirstOrDefault(g => g.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                groups.Remove(existing);
                groups.Add(updatedGroup);
                SaveGroupDetails(root, groups);

                // If name changed, update apps using that group
                if (!oldName.Equals(updatedGroup.Name, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateAppsGroupName(root, oldName, updatedGroup.Name);
                }
            }
        }

        private static void UpdateAppsGroupName(XElement root, string oldName, string newName)
        {
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

        public static GroupDetails GetGroupDetails(XElement root, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return null;
            return LoadGroupDetails(root).FirstOrDefault(g => 
                g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
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
