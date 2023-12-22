using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace GoldbergCommon
{
    public class ParseController
    {
        public static readonly List<string> KeymapDigitaldefault = new List<string>
        {
            "_PAD_UP_=DUP",
            "_PAD_DOWN_=DDOWN",
            "_PAD_LEFT_=DLEFT",
            "_PAD_RIGHT_=DRIGHT",
            "_PAD_START_=START",
            "_PAD_BACK_=BACK",
            "_PAD_A_=A",
            "_PAD_B_=B",
            "_PAD_X_=X",
            "_PAD_Y_=Y",
            "_PAD_LB_=LBUMPER",
            "_PAD_LT_=DLTRIGGER",
            "_PAD_LBT_=LSTICK",
            "_PAD_RB_=RBUMPER",
            "_PAD_RT_=DRTRIGGER",
            "_PAD_RBT_=RSTICK",
            "_AN_STICK_L_=LJOY=joystick_move",
            "_AN_STICK_R_=RJOY=joystick_move"
        };
        public static readonly Dictionary<string, string> KeymapDigital = new Dictionary<string, string>
        {
            { "button_a", "A" },
            { "button_b", "B" },
            { "button_x", "X" },
            { "button_y", "Y" },
            { "dpad_north", "DUP" },
            { "dpad_south", "DDOWN" },
            { "dpad_east", "DRIGHT" },
            { "dpad_west", "DLEFT" },
            { "button_escape", "START" },
            { "button_menu", "BACK" },
            { "left_bumper", "LBUMPER" },
            { "right_bumper", "RBUMPER" },
            { "button_back_left", "A" },
            { "button_back_right", "X" },
        };
        public static Dictionary<string, List<string>> Parse(KeyValue controllerMapping)
        {
            var groups = controllerMapping.Children.Where(x => x.Name.Equals("group")).ToList();
            Dictionary<string, KeyValue> groupsById = new Dictionary<string, KeyValue>();
            foreach (var g in groups)
            {
                groupsById.Add(g["id"].Value, g);
            }
            var actions = controllerMapping["actions"];
            List<string> action_list = new List<string>();
            foreach (var a in actions.Children)
            {
                action_list.Add(a.Name);
            }
            var presets = controllerMapping.Children.Where(x => x.Name.Equals("preset")).ToList();
            var all_bindings = new Dictionary<string, List<string>>();
            string binding = string.Empty;
            // get actions list by presets if there are no action list.
            if (action_list.Count <= 0)
            {
                presets.ForEach(x => action_list.Add(x.Children.FirstOrDefault(c => c.Name.Equals("name")).Value));
            }
            foreach (var p in presets)
            {
                var name = p.Children.FirstOrDefault(x => x.Name.Equals("name")).Value;
                if (!action_list.Contains(name))
                {
                    continue;
                }
                var group_bindings = p.Children.FirstOrDefault(x => x.Name.Equals("group_source_bindings"));
                Dictionary<string, KeyValue> bindings = new Dictionary<string, KeyValue>();
                foreach (var number in group_bindings.Children)
                {
                    var s = group_bindings[number.Name].Value.Split();
                    if (s.Length > 1 && s[1] != "active")
                    {
                        continue;
                    }
                    if (new List<string> { "switch", "button_diamond", "dpad" }.Contains(s[0]))
                    {
                        // Your code here
                        var group = groupsById[number.Name];

                        bindings = InputBinding(group, bindings, keymap: KeymapDigital);
                    }
                    if (new List<string> { "left_trigger", "right_trigger" }.Contains(s[0]))
                    {
                        var group = groupsById[number.Name];
                        if (group["mode"].Value == "trigger")
                        {
                            foreach (var g in group.Children)
                            {
                                if (g.Name == "gameactions")
                                {
                                    var pas = group["gameactions"];
                                    var action_name = group["gameactions"][name].Value;
                                    if (s[0] == "left_trigger")
                                    {
                                        binding = "LTRIGGER";
                                    }
                                    else
                                    {
                                        binding = "RTRIGGER";
                                    }
                                    if (bindings.ContainsKey(action_name))
                                    {
                                        if (!bindings[action_name].Name.Contains(binding) && !bindings[action_name].Name.Contains(binding + "=trigger"))
                                        {
                                            bindings[action_name].Name = $"{binding},{bindings[action_name].Name}";
                                        }
                                    }
                                    else
                                    {
                                        bindings.Add(action_name, new KeyValue(binding + "=trigger"));
                                    }
                                }
                                if (g.Name == "inputs")
                                {
                                    if (s[0] == "left_trigger")
                                    {
                                        binding = "DLTRIGGER";
                                    }
                                    else
                                    {
                                        binding = "DRTRIGGER";
                                    }
                                    bindings = InputBinding(group, bindings, keymap: KeymapDigital, binding);
                                }
                            }
                        }
                    }
                    if (new List<string> { "joystick", "right_joystick", "dpad" }.Contains(s[0]))
                    {
                        var group = groupsById[number.Name];
                        if (group["mode"].Value == "joystick_move")
                        {
                            foreach (var g in group.Children)
                            {
                                if (g.Name == "gameactions")
                                {
                                    var action_name = group["gameactions"][name].Value;
                                    if (s[0] == "joystick")
                                    {
                                        binding = "LJOY";
                                    }
                                    else if (s[0] == "right_joystick")
                                    {
                                        binding = "RJOY";
                                    }
                                    else if (s[0] == "dpad")
                                    {
                                        binding = "DPAD";
                                    }
                                    if (bindings.ContainsKey(action_name))
                                    {
                                        if (!bindings[action_name].Name.Contains(binding) && !bindings[action_name].Name.Contains(binding + "=joystick_move"))
                                        {
                                            bindings[action_name].Name = $"{binding},{bindings[action_name].Name}";
                                        }
                                    }
                                    else
                                    {
                                        bindings.Add(action_name, new KeyValue(binding + "=joystick_move"));
                                    }
                                }
                                if (g.Name == "inputs")
                                {
                                    if (s[0] == "joystick")
                                    {
                                        binding = "LSTICK";
                                    }
                                    else
                                    {
                                        binding = "RSTICK";
                                    }
                                    bindings = InputBinding(group, bindings, KeymapDigital, binding);
                                }
                            }
                        }
                        else if (group["mode"].Value == "dpad")
                        {
                            if (s[0] == "joystick")
                            {
                                bindings = InputBinding(group, bindings, keymap: new Dictionary<string, string> {
                                    { "dpad_north", "DLJOYUP" },
                                    { "dpad_south", "DLJOYDOWN" },
                                    {"dpad_west", "DLJOYLEFT" },
                                    {"dpad_east","DLJOYRIGHT" },
                                    { "click", "LSTICK" }
                                });
                            }
                            else if (s[0] == "right_joystick")
                            {
                                bindings = InputBinding(group, bindings, keymap: new Dictionary<string, string> {
                                    { "dpad_north", "DRJOYUP" },
                                    { "dpad_south", "DRJOYDOWN" },
                                    {"dpad_west", "DRJOYLEFT" },
                                    {"dpad_east","DRJOYRIGHT" },
                                    { "click", "RSTICK" }
                                });
                            }
                        }
                    }
                }
                bindings.ForEach(x =>
                {
                    if (!all_bindings.ContainsKey(name))
                    {
                        all_bindings.Add(name, new List<string> { x.Key + "=" + x.Value.Name });
                    }
                    all_bindings[name].Add(x.Key + "=" + x.Value.Name);
                });
            }
            if (all_bindings.Values.Count == 0)
            {
                all_bindings = new Dictionary<string, List<string>> { { "MenuControls", KeymapDigitaldefault } };
            }
            return all_bindings;
        }
        static Dictionary<string, KeyValue> InputBinding(KeyValue group, Dictionary<string, KeyValue> bindings, Dictionary<string, string> keymap, string forceBinding = null)
        {
            foreach (var i in group["inputs"].Children)
            {
                foreach (var act in group["inputs"][i.Name].Children)
                {
                    foreach (var fp in group["inputs"][i.Name][act.Name].Children)
                    {
                        foreach (var bd in group["inputs"][i.Name][act.Name][fp.Name].Children)
                        {
                            foreach (var bbd in group["inputs"][i.Name][act.Name][fp.Name][bd.Name].Children)
                            {
                                if (bbd.Name == "binding")
                                {
                                    var st = bbd.Value.Split();
                                    if (st[0] == "game_action")
                                    {
                                        string actionName;
                                        if (st[2].EndsWith(","))
                                        {
                                            actionName = st[2].Substring(0, st[2].Length - 1);
                                        }
                                        else
                                        {
                                            actionName = st[2];
                                        }

                                        string binding;
                                        if (forceBinding == null && keymap.ContainsKey(i.Name.ToLower()))
                                        {
                                            binding = keymap[i.Name.ToLower()];
                                        }
                                        else
                                        {
                                            binding = forceBinding;
                                        }

                                        if (bindings != null && bindings.ContainsKey(actionName))
                                        {
                                            if (!bindings[actionName].Name.Contains(binding))
                                            {
                                                bindings[actionName].Name = bindings[actionName].Name + "," + binding;
                                            }
                                        }
                                        else
                                        {
                                            bindings.Add(actionName, new KeyValue(binding));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return bindings;
        }
    }
}
