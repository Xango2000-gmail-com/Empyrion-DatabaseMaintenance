using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eleon.Modding;

namespace DatabaseMaintenance
{
    class CommonFunctions
    {
        internal class ItemStackX
        {
            internal int slotIdx;
            internal int id;
            internal long count;
            internal int ammo;
            internal int decay;
        }

        internal static void LogFile(string FileName, string FileData)
        {
            if (!File.Exists(MyEmpyrionMod.ModPath + FileName))
            {
                try
                {
                    using (FileStream fs = File.Create(MyEmpyrionMod.ModPath + FileName)) { }
                }
                catch
                {
                    File.WriteAllText(MyEmpyrionMod.ModPath + "Debug.txt", "File Doesnt Exist");
                }
            }
            File.AppendAllText(MyEmpyrionMod.ModPath + FileName, FileData + Environment.NewLine);
        }

        internal static void Debug(string Data)
        {
            if (MyEmpyrionMod.debug)
            {
                LogFile("Debug.txt", Data);
            }
        }

        internal static void Log(string Data)
        {
            LogFile("Logs\\Log " + MyEmpyrionMod.BootupTimestamp + ".txt", Data);
        }

        internal static void Log(string Data, string Process)
        {
            LogFile("Logs\\" + Process + " " + MyEmpyrionMod.BootupTimestamp + ".txt", Data);
        }

        internal static int SeqNrGenerator(int LastSeqNr)
        {
            bool Fail = false;
            int CurrentSeqNr = 2000;
            do
            {
                if (LastSeqNr > 65530)
                {
                    LastSeqNr = 2000;
                }
                CurrentSeqNr = LastSeqNr + 1;
                if (MyEmpyrionMod.SeqNrStorage.ContainsKey(CurrentSeqNr)) { Fail = true; }
            } while (Fail == true);
            return CurrentSeqNr;
        }

        internal static string ArrayConcatenate(int start, string[] array)
        {
            string message = "";
            for (int i = start; i < array.Length; i++)
            {
                message = message + "\r\n";
                message = message + array[i];
            }
            return message;
        }

        internal static string ArrayToString(int start, string[] array)
        {
            string message = "";
            for (int i = start; i < array.Length; i++)
            {
                message = message + " " + array[i];
            }
            return message;
        }

        public static void FileReader(ushort ThisSeqNr, string File)
        {
            //Checks for simple errors
            string[] Script1 = System.IO.File.ReadAllLines(File);
            for (int i = 0; i < Script1.Count(); ++i)
            {

            }
        }

        public static string ChatmessageHandler(string[] Chatmessage, string Selector)
        {
            List<string> Restring = new List<string>(Chatmessage);
            string Picked = "";
            if (Selector.Contains('*'))
            {
                if (Selector == "1*")
                {
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "2*")
                {
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "3*")
                {
                    Restring.Remove(Restring[2]);
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "4*")
                {
                    Restring.Remove(Restring[3]);
                    Restring.Remove(Restring[2]);
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
                else if (Selector == "5*")
                {
                    Restring.Remove(Restring[4]);
                    Restring.Remove(Restring[3]);
                    Restring.Remove(Restring[2]);
                    Restring.Remove(Restring[1]);
                    Restring.Remove(Restring[0]);
                    Picked = string.Join(" ", Restring.ToArray());
                }
            }
            else
            {

            }
            return Picked;
        }

        public static Dictionary<string, string[]> CSVReader1(string File)
        {
            Dictionary<string, string[]> ItemDB = new Dictionary<string, string[]> { };
            string[] Line = System.IO.File.ReadAllLines(File);
            foreach (string Item in Line)
            {
                string[] itemArray = Item.Split(',');
                ItemDB.Add(itemArray[0], itemArray);
            }
            return ItemDB;
        }

        public static ItemStack[] ReadItemStacks(string FolderPath, string FileName)
        {
            string RealFile = MyEmpyrionMod.ModPath + FolderPath + FileName;
            string[] bagLines = System.IO.File.ReadAllLines(RealFile);
            List<string> Group1 = bagLines.ToList();
            if (System.IO.File.Exists(MyEmpyrionMod.ModPath + FolderPath + "Cache.txt"))
            {
                try
                {
                    string[] CacheLines = System.IO.File.ReadAllLines(MyEmpyrionMod.ModPath + FolderPath + "Cache.txt");
                    List<string> Group2 = CacheLines.ToList();
                    foreach (string GroupItem in Group2)
                    {
                        Group1.Add(GroupItem);
                    }
                }
                catch
                {
                    Debug("cache.txt file read error");
                }
            }
            bagLines = Group1.ToArray();
            //int itemStackSize = bagLines.Count();
            List<ItemStack> ItemStackList = new List<ItemStack> { };
            List<ItemStack> ReCacheList = new List<ItemStack> { };
            int LineCounter = 0;
            foreach (string Line in bagLines)
            {
                if (Line.Contains(','))
                {
                    string[] SplitLine = Line.Split(',');
                    ItemStack NewEntry = new ItemStack
                    {
                        slotIdx = Byte.Parse("0"),
                        id = Int32.Parse(SplitLine[1]),
                        count = Int32.Parse(SplitLine[2]),
                        ammo = Int32.Parse(SplitLine[3]),
                        decay = Int32.Parse(SplitLine[4])
                    };
                    if (LineCounter < 50)
                    {
                        ItemStackList.Add(NewEntry);
                    }
                    else
                    {
                        ReCacheList.Add(NewEntry);
                    }
                    LineCounter = LineCounter + 1;
                }
            }
            ItemStack[] itStack = ItemStackList.ToArray();
            try
            {
                if (ReCacheList.Count() != 0)
                {
                    ItemStack[] ReCache = ReCacheList.ToArray();
                    WriteItemStacksSimple(FolderPath, "Cache.txt", ReCache);
                }
                else
                {
                    File.WriteAllText(MyEmpyrionMod.ModPath + FolderPath + "Cache.txt", "");
                }
            }
            catch
            {
                Debug("Failed writing cache file");
            }
            return itStack;
        }

        public static void WriteItemStacksSimple(string FolderPath, string FileName, ItemStack[] ItemStacks)
        {
            File.WriteAllText(MyEmpyrionMod.ModPath + FolderPath + FileName, "");
            foreach (ItemStack item in ItemStacks)
            {
                LogFile(FolderPath + FileName, item.slotIdx + "," + item.id + "," + item.count + "," + item.decay + "," + item.ammo);
            }
        }

        public static void WriteItemStacks(string FolderPath, string FileName, ItemStack[] ItemStacks, string SuperStack, int MaxSuperStackSize)
        {
            Dictionary<int, List<ItemStackX>> SuperStackerRoundOne = new Dictionary<int, List<ItemStackX>> { }; //item.id, List<ItemStacks>
            Dictionary<int, List<ItemStackX>> SuperStackerRoundTwo = new Dictionary<int, List<ItemStackX>> { };
            if (SuperStack.ToLower() == "true")
            {
                int MaxSuperstack = 100000000;
                if (MaxSuperStackSize < MaxSuperstack + 1)
                {
                    MaxSuperstack = MaxSuperStackSize;
                }
                //Round 1: Superstack Everything
                int ItemStacksCount = ItemStacks.Count();
                foreach (ItemStack Stack in ItemStacks)
                {
                    List<ItemStackX> ListItemStackX2 = new List<ItemStackX> { };
                    if (SuperStackerRoundOne.Keys.Contains(Stack.id))
                    {
                        List<ItemStackX> ListItemStackX = SuperStackerRoundOne[Stack.id];
                        foreach (ItemStackX ItemStackx in ListItemStackX)
                        {
                            if (Stack.ammo == ItemStackx.ammo && Stack.decay == ItemStackx.decay)
                            {
                                ItemStackX NewItemStack = new ItemStackX
                                {
                                    count = Stack.count + ItemStackx.count,
                                    id = Stack.id,
                                    ammo = Stack.ammo,
                                    decay = Stack.decay,
                                    slotIdx = Stack.slotIdx
                                };
                                ListItemStackX2.Add(NewItemStack);
                            }
                            else
                            {
                                ListItemStackX2.Add(ItemStackx);
                            }
                        }
                    }
                    else
                    {
                        //if item not in the list yet, just add it
                        ItemStackX NewItemStack = new ItemStackX
                        {
                            count = Stack.count,
                            id = Stack.id,
                            ammo = Stack.ammo,
                            decay = Stack.decay,
                            slotIdx = Stack.slotIdx
                        };
                        ListItemStackX2.Add(NewItemStack);
                    }
                    SuperStackerRoundOne[Stack.id] = ListItemStackX2;
                }
                //Round 2: Reduce stack sizes to MaxSuperstack
                //foreach itemID, foreach item in list, split stack till under maxSuperStack and add into new list, save new list
                foreach (List<ItemStackX> ListISx in SuperStackerRoundOne.Values)
                {
                    foreach (ItemStackX ISx in ListISx)
                    {
                        List<ItemStackX> ListItemStackX3 = new List<ItemStackX> { };
                        if (ISx.count > MaxSuperstack)
                        {
                            long StackCount = ISx.count;
                            while (StackCount > MaxSuperstack)
                            {
                                // add capped stack to list
                                ItemStackX NewISx = new ItemStackX
                                {
                                    count = MaxSuperstack,
                                    id = ISx.id,
                                    ammo = ISx.ammo,
                                    decay = ISx.decay,
                                    slotIdx = ISx.slotIdx
                                };
                                ListItemStackX3.Add(NewISx);
                                StackCount = StackCount - MaxSuperstack;
                            }
                            //add remainder to list
                            ItemStackX NewItemStack = new ItemStackX
                            {
                                count = StackCount,
                                id = ISx.id,
                                ammo = ISx.ammo,
                                decay = ISx.decay,
                                slotIdx = ISx.slotIdx
                            };
                            ListItemStackX3.Add(NewItemStack);
                        }
                        else
                        {
                            //just add the stack to the list
                            ItemStackX NewItemStack = new ItemStackX
                            {
                                count = ISx.count,
                                id = ISx.id,
                                ammo = ISx.ammo,
                                decay = ISx.decay,
                                slotIdx = ISx.slotIdx
                            };
                            ListItemStackX3.Add(NewItemStack);
                        }
                        //save new list to SuperStackRoundTwo
                        SuperStackerRoundTwo[ISx.id] = ListItemStackX3;
                    }
                }
            }
            else
            {
                //if superstack = false
                foreach (ItemStack stack in ItemStacks)
                {
                    List<ItemStackX> ListItemStackX2 = new List<ItemStackX> { };
                    ItemStackX NewItemStack = new ItemStackX
                    {
                        count = stack.count,
                        id = stack.id,
                        ammo = stack.ammo,
                        decay = stack.decay,
                        slotIdx = stack.slotIdx
                    };
                    ListItemStackX2.Add(NewItemStack);
                    SuperStackerRoundOne[stack.id] = ListItemStackX2;
                }
                SuperStackerRoundTwo = SuperStackerRoundOne;
            }
            // Round 3: Write To File
            File.WriteAllText(MyEmpyrionMod.ModPath + FolderPath + FileName, "");
            int StackCounter = 0;
            foreach (List<ItemStackX> SuperStacker1 in SuperStackerRoundTwo.Values)
            {
                foreach (ItemStackX ItemStackOne in SuperStacker1)
                {
                    if (StackCounter < 50)
                    {
                        LogFile(FolderPath + FileName, ItemStackOne.slotIdx + "," + ItemStackOne.id + "," + ItemStackOne.count + "," + ItemStackOne.decay + "," + ItemStackOne.ammo);
                    }
                    else
                    {
                        LogFile(FolderPath + "Cache.txt", ItemStackOne.slotIdx + "," + ItemStackOne.id + "," + ItemStackOne.count + "," + ItemStackOne.decay + "," + ItemStackOne.ammo);
                    }
                    StackCounter++;
                }
            }
        }

        public static string SplitChat2(string ChatMessage)
        {
            string[] splitted = ChatMessage.Split(new[] { ' ' }, 2);
            string message = splitted[1];
            return message;
        }

        public static string UnixTimeStamp()
        {
            string time = Convert.ToString((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
            return time;
        }

        public static string TimeStamp()
        {
            //string Timestamp = CommonFunctions.UnixTimeStamp();
            DateTime Timestamp2 = CommonFunctions.UnixTimeStampToDateTime(Convert.ToDouble(CommonFunctions.UnixTimeStamp()));
            return Timestamp2.ToString("yyyy/MM/dd HH:mm:ss");
        }

        public static string TimeStampFilename()
        {
            //string Timestamp = CommonFunctions.UnixTimeStamp();
            DateTime Timestamp2 = CommonFunctions.UnixTimeStampToDateTime(Convert.ToDouble(CommonFunctions.UnixTimeStamp()));
            return Timestamp2.ToString("yyyy.MM.dd HH.mm");
        }
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        static List<char> GoodCharacters = new List<char> { '/', ':', ',', '.', ' ', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public static string Sanitize(string Input)
        {
            string Output = "";
            foreach (char Character in Input)
            {
                if (Character == '\'')
                {
                    Output = Output + "\'" + Character.ToString();
                }
                else if (Character == '\"')
                {
                    Output = Output + "\"" + Character.ToString();
                }
                else if (GoodCharacters.Contains(Character))
                {
                    Output = Output + Character.ToString();
                }
                else
                {
                    Output = Output + "\\" + Character.ToString();
                }
            }
            //CommonFunctions.Log(Output);
            return Output;
        }
    }
}

