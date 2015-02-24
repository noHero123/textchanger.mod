using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonFx.Json;
using System.Threading;
using System.Net;
using System.IO;
using System.Reflection;

namespace TranslationTool.mod
{
    public struct mappedstringding
    {
        public string key;
        public string value;
    }

    public struct achivementding
    {
        public string id;
        public string name;
        public string description;
        public string oname;
        public string odescription;
    }

    public struct trialding
    {
        public string id;
        public string name;
        public string description;
        public string flavour;
        public string title;

        public string oname;
        public string odescription;
        public string oflavour;
        public string otitle;
    }

    class CardTextTranslator
    {
        private string pathToConfig = "";

        public string chatroom = "general-10";
        public Dictionary<string, string> googlekeys = new Dictionary<string, string>();

        public Dictionary<string, string> googleAchivementkeys = new Dictionary<string, string>();

        public Dictionary<string, string> googleTutorialkeys = new Dictionary<string, string>();

        CardTypesMessage orginalcards;
        MappedStringsMessage orginalmappedstrings;

        Dictionary<string, achivementding> achivementlist = new Dictionary<string, achivementding>();
        AchievementTypesMessage orginalAchivements;

        Dictionary<string, trialding> triallist = new Dictionary<string, trialding>();
        //dont need the orginal ones, because they are sended every time we click on it-> translate them on arrive!


        private List<string> untranslatedcard = new List<string>();

        public List<string> id = new List<string>();
        public List<string> names = new List<string>();
        public List<string> desc = new List<string>();
        public List<string> flavor = new List<string>();
        public List<string> translatedDesc = new List<string>();

        public bool notTranslatedScrolls = false;
        List<string> oid = new List<string>();
        List<string> onames = new List<string>();
        List<string> odesc = new List<string>();
        List<string> oflavor = new List<string>();
        Dictionary<string, string> oMappedStrings = new Dictionary<string, string>();

        List<CardType.TypeSet> otypes = new List<CardType.TypeSet>();
        List<string[]> oactiveAbilitys = new List<string[]>();
        List<string[]> opassiveAbilitys = new List<string[]>();
        List<string[]> opassiveDescs = new List<string[]>();
        Dictionary<string, string> translatedPieceKind = new Dictionary<string, string>();
        Dictionary<string, string> translatedActiveAbility = new Dictionary<string, string>();
        Dictionary<string, string> translatedPieceType = new Dictionary<string, string>();
        Dictionary<string, string> translatedPassiveAbility = new Dictionary<string, string>();
        Dictionary<string, string> translatedPassiveDescription = new Dictionary<string, string>();
        List<mappedstringding> translatedMappedStrings = new List<mappedstringding>();

        Settings sttngs;

        public CardTextTranslator(string path, Settings s)
        {
            this.sttngs = s;
            this.pathToConfig = path;
        }

        public string getDataFromGoogleDocs(string googledatakey)
        {
            WebRequest myWebRequest;

            //https://docs.google.com/spreadsheet/pub?key=0AhhxijYPL-BGdDJaWTI4UVJ3OUZfYzlCSWo3dkZSOXc&output=txt
            myWebRequest = WebRequest.Create("https://spreadsheets.google.com/feeds/list/" + googledatakey + "/od6/public/values?alt=json");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            
            myWebRequest.Timeout = 10000;
            string ressi = "error";

            int loaded = 0;
            while (loaded < 5)
            {
                try
                {
                    WebResponse myWebResponse = myWebRequest.GetResponse();
                    System.IO.Stream stream = myWebResponse.GetResponseStream();
                    System.IO.StreamReader reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
                    ressi = reader.ReadToEnd();

                    loaded = 10;
                }
                catch
                {
                    loaded++;
                    if (loaded >= 4) myWebRequest.Timeout = 20000;
                    Console.WriteLine("timeout/error");
                }
            }

            return ressi;
        }



        public void readJsonfromGoogle(string txt)
        {
            //Console.WriteLine(txt);
            this.names.Clear();
            this.desc.Clear();
            this.flavor.Clear();
            this.id.Clear();
            this.translatedDesc.Clear();
            JsonReader jsonReader = new JsonReader();
            Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(txt);
            dictionary = (Dictionary<string, object>)dictionary["feed"];
            Dictionary<string, object>[] entrys = (Dictionary<string, object>[])dictionary["entry"];
            for (int i = 0; i < entrys.GetLength(0); i++)
            {
                /*for (int j = 0; j < 4; j++)
                {
                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$r"+(j+1)];
                    Console.WriteLine((string)dictionary["$t"]);
                    
                }*/

                dictionary = (Dictionary<string, object>)entrys[i]["gsx$id"];
                if (((string)dictionary["$t"]).ToLower() != "")
                {
                    this.id.Add((string)dictionary["$t"]);
                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$name"];
                    this.names.Add((string)dictionary["$t"]);
                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$description"];
                    this.desc.Add((string)dictionary["$t"]);
                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$flavor"];
                    this.flavor.Add((string)dictionary["$t"]);

                    if (((Dictionary<string, object>)entrys[i]).ContainsKey("gsx$orginaldescription"))
                    {
                        dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginaldescription"];
                        this.translatedDesc.Add((string)dictionary["$t"]);
                    }
                }

            }


        }

        public void readAchiveJsonfromGoogle(string txt)
        {
            //Console.WriteLine(txt);
            this.achivementlist.Clear();

            JsonReader jsonReader = new JsonReader();
            Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(txt);
            dictionary = (Dictionary<string, object>)dictionary["feed"];
            Dictionary<string, object>[] entrys = (Dictionary<string, object>[])dictionary["entry"];
            for (int i = 0; i < entrys.GetLength(0); i++)
            {

                dictionary = (Dictionary<string, object>)entrys[i]["gsx$id"];
                
                if (((string)dictionary["$t"]).ToLower() != "")
                {
                    string id = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedname"];
                    string tname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginalname"];
                    string oname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translateddescription"];
                    string tdesc = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginaldescription"];
                    string odesc = ((string)dictionary["$t"]);

                    achivementding a = new achivementding();
                    a.id = id;
                    a.name = tname;
                    a.oname = oname;
                    a.description = tdesc;
                    a.odescription = odesc;

                    this.achivementlist.Add(id, a);
                }

            }


        }

        public void readTrialJsonfromGoogle(string txt)
        {
            //Console.WriteLine(txt);
            this.triallist.Clear();

            JsonReader jsonReader = new JsonReader();
            Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(txt);
            dictionary = (Dictionary<string, object>)dictionary["feed"];
            Dictionary<string, object>[] entrys = (Dictionary<string, object>[])dictionary["entry"];
            for (int i = 0; i < entrys.GetLength(0); i++)
            {

                dictionary = (Dictionary<string, object>)entrys[i]["gsx$id"];

                if (((string)dictionary["$t"]).ToLower() != "")
                {
                    string id = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedname"];
                    string tname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginalname"];
                    string oname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translateddescription"];
                    string tdesc = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginaldescription"];
                    string odesc = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedflavour"];
                    string tflav = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginalflavour"];
                    string oflav = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedtitel"];
                    string ttitle = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginaltitle"];
                    string otitle= ((string)dictionary["$t"]);

                    trialding a = new trialding();
                    a.id = id;
                    a.name = tname;
                    a.oname = oname;
                    a.description = tdesc;
                    a.odescription = odesc;
                    a.flavour = tflav;
                    a.oflavour = oflav;
                    a.title = ttitle;
                    a.otitle = otitle;

                    this.triallist.Add(id, a);
                }

            }


        }




        public void readJsonfromGoogleFast(string txt)
        {
            //Console.WriteLine(txt);
            this.names.Clear();
            this.desc.Clear();
            this.flavor.Clear();
            this.id.Clear();
            this.translatedDesc.Clear();

            String[] lines = txt.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                /*for (int j = 0; j < 4; j++)
                {
                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$r"+(j+1)];
                    Console.WriteLine((string)dictionary["$t"]);
                    
                }*/

                string[] data = lines[i].Split(new string[] { "\t" }, StringSplitOptions.None);
                if(data.Length >=1) this.id.Add(data[0]);
                if (data.Length >= 2) this.names.Add(data[1]);
                if (data.Length >= 3) this.desc.Add(data[2]);
                if (data.Length >= 4) this.flavor.Add(data[3]);
                if (data.Length >= 5) this.translatedDesc.Add(data[4]);

            }


        }

        public void readAchiveJsonfromGoogleFast(string txt)
        {
            //Console.WriteLine(txt);
            this.achivementlist.Clear();

            JsonReader jsonReader = new JsonReader();
            Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(txt);
            dictionary = (Dictionary<string, object>)dictionary["feed"];
            Dictionary<string, object>[] entrys = (Dictionary<string, object>[])dictionary["entry"];
            for (int i = 0; i < entrys.GetLength(0); i++)
            {

                dictionary = (Dictionary<string, object>)entrys[i]["gsx$id"];

                if (((string)dictionary["$t"]).ToLower() != "")
                {
                    string id = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedname"];
                    string tname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginalname"];
                    string oname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translateddescription"];
                    string tdesc = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginaldescription"];
                    string odesc = ((string)dictionary["$t"]);

                    achivementding a = new achivementding();
                    a.id = id;
                    a.name = tname;
                    a.oname = oname;
                    a.description = tdesc;
                    a.odescription = odesc;

                    this.achivementlist.Add(id, a);
                }

            }


        }

        public void readTrialJsonfromGoogleFast(string txt)
        {
            //Console.WriteLine(txt);
            this.triallist.Clear();

            JsonReader jsonReader = new JsonReader();
            Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(txt);
            dictionary = (Dictionary<string, object>)dictionary["feed"];
            Dictionary<string, object>[] entrys = (Dictionary<string, object>[])dictionary["entry"];
            for (int i = 0; i < entrys.GetLength(0); i++)
            {

                dictionary = (Dictionary<string, object>)entrys[i]["gsx$id"];

                if (((string)dictionary["$t"]).ToLower() != "")
                {
                    string id = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedname"];
                    string tname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginalname"];
                    string oname = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translateddescription"];
                    string tdesc = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginaldescription"];
                    string odesc = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedflavour"];
                    string tflav = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginalflavour"];
                    string oflav = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$translatedtitel"];
                    string ttitle = ((string)dictionary["$t"]);

                    dictionary = (Dictionary<string, object>)entrys[i]["gsx$orginaltitle"];
                    string otitle = ((string)dictionary["$t"]);

                    trialding a = new trialding();
                    a.id = id;
                    a.name = tname;
                    a.oname = oname;
                    a.description = tdesc;
                    a.odescription = odesc;
                    a.flavour = tflav;
                    a.oflavour = oflav;
                    a.title = ttitle;
                    a.otitle = otitle;

                    this.triallist.Add(id, a);
                }

            }


        }


        private string getPieceTypes(Card c)
        {
            string retu = c.getPieceType();
            foreach (KeyValuePair<string, string> kvp in this.translatedPieceType)
            { retu = retu.Replace(kvp.Key, kvp.Value); }

            return retu;

        }

        private int getindexfromcardtypearray(CardType[] cts, int cardid)
        {
            int retval = -1;

            for (int i = 0; i <= cts.Length - 1; i++)
            {
                if (cts[i].id == cardid)
                {
                    retval = i;
                    break; // break passt schon :P
                }
            }
            return retval;
        }

        private string changePieceKind(string s)
        {
            string retu = s;
            if (this.translatedPieceKind.ContainsKey(retu))
            { retu = translatedPieceKind[retu]; }
            return retu;
        }

        private string changePieceTypes(string s)
        {
            string retu = s;
            foreach (KeyValuePair<string, string> kvp in this.translatedPieceType)
            { retu = retu.Replace(kvp.Key, kvp.Value); }

            return retu;

        }

        private string changeActiveAbilities(string s)
        {
            string retu = s;

            if (!(s == "Move") && s != null)
            {
                if (this.translatedActiveAbility.ContainsKey(s))
                { retu = this.translatedActiveAbility[s]; }
            }

            return retu;

        }

        private string changePassiveAbilities(string s)
        {
            string retu = s;
            if (s != null)
            {
                if (this.translatedPassiveAbility.ContainsKey(s))
                { retu = this.translatedPassiveAbility[s]; }
            }
            if (s == retu)
            {
                foreach (string r in s.Split(' '))
                {

                    if (r != null)
                    {
                        if (this.translatedPassiveAbility.ContainsKey(r))
                        { retu = retu.Replace(r, this.translatedPassiveAbility[r]); }

                    }

                }
            }
            return retu;

        }

        private string changePassiveDescription(string s)
        {
            string retu = s;

            if (s != null)
            {
                if (this.translatedPassiveDescription.ContainsKey(s))
                { retu = this.translatedPassiveDescription[s]; }
            }

            if (s == retu)
            {
                foreach (string r in s.Split(' '))
                {

                    if (r != null)
                    {
                        if (this.translatedPassiveDescription.ContainsKey(r))
                        { retu = retu.Replace(r, this.translatedPassiveDescription[r]); }

                    }

                }
            }
            return retu;

        }

        private void setCardtexts()
        {
            clearDictionaries();
            

            CardType[] cts = new CardType[this.orginalcards.cardTypes.Length];
            this.orginalcards.cardTypes.CopyTo(cts, 0);

            //mapped strings!!!
            
            MappedString[] newmappedstrings = new MappedString[this.orginalmappedstrings.strings.Length];
            this.orginalmappedstrings.strings.CopyTo(newmappedstrings, 0);

            Console.WriteLine("copied mappped strings");

            bool traDescs = false;
            if (this.translatedDesc.Count > 0)
            { traDescs = true; }

            this.untranslatedcard.Clear();
            this.notTranslatedScrolls = false;

            for (int i = 0; i < this.id.Count; i++)
            {
                int cardid = Convert.ToInt32(this.id[i]);//no need, but why not?:D
                string cardname = this.names[i];
                string description = this.desc[i];
                string flavor = this.flavor[i];
                string transDesc = "";
                if (traDescs)
                {
                    transDesc = translatedDesc[i];
                }

                //get index from cts
                if (cardid == 99999)// its a kind we change
                {
                    this.translatedPieceKind.Add(cardname, description);
                }

                if (cardid == 88888)// its a type we change
                {
                    this.translatedPieceType.Add(cardname, description);
                }

                if (cardid == 77777)// its a ActiveAb. we change
                {
                    this.translatedActiveAbility.Add(cardname, description);
                }
                if (cardid == 66666)// its a ActiveAb. we change
                {
                    this.translatedPassiveAbility.Add(cardname, description);
                }

                if (cardid == 55555)// its a ActiveAb. we change
                {
                    this.translatedPassiveDescription.Add(cardname, description);
                }

                if (cardid == 44444)// its a MappedString!
                {
                    Console.WriteLine("add mapped string" + cardname);
                    mappedstringding msd = new mappedstringding();
                    msd.key = cardname;
                    msd.value = description;
                    //flavor = orginal key-name

                    bool foundDescription = false;

                    foreach (string s in this.oMappedStrings.Values)
                    {
                        if (s == transDesc)
                        {
                            foundDescription = true;
                            break;
                        }
                    }

                    if (foundDescription)
                    {
                        //Console.WriteLine("add mapped string " + cardname + " " + description + " " + flavor);
                        this.translatedMappedStrings.Add(msd);
                    }
                    else 
                    {
                        Console.WriteLine("### " + flavor + " mappedstring was changed, so it is not translated");

                    }
                }

                int ctsindex = getindexfromcardtypearray(cts, cardid);
                //change description
                if (ctsindex >= 0)
                {
                    if (transDesc == cts[ctsindex].description)
                    { 
                        cts[ctsindex].description = description; 
                    }
                    else
                    {
                        Console.WriteLine("## " + cardname + " description was changed, so it is not translated");
                        this.untranslatedcard.Add(cardname);
                        this.notTranslatedScrolls = true;
                    }
                    //change flavor
                    cts[ctsindex].flavor = flavor;
                }




            }
            Console.WriteLine("change cts");
            for (int i = 0; i < this.id.Count; i++)
            {
                //change the other stuff of the cards (loaded with ids 66666-99999)
                int cardid = Convert.ToInt32(this.id[i]);//no need, but why not?:D
                int ctsindex = getindexfromcardtypearray(cts, cardid);
                //change description
                if (ctsindex >= 0)
                {
                    cts[ctsindex].types = new CardType.TypeSet(changePieceTypes(cts[ctsindex].types.ToString()).Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries));
                    foreach (ActiveAbility a in cts[ctsindex].abilities)
                    {
                        a.name = this.changeActiveAbilities(a.name);
                    }
                    foreach (PassiveAbility a in cts[ctsindex].passiveRules)
                    {
                        a.displayName = this.changePassiveAbilities(a.displayName);
                        a.description = this.changePassiveDescription(a.description);
                    }

                }
            }

            
            //change mapped strings
            
            List<MappedString> mappedstringlist = new List<MappedString>();
            Console.WriteLine("change mappedstringlist");
            //add orginal mapped strings first
            foreach(MappedString ms in newmappedstrings)
            {
                string key = ms.key;
                string value = ms.value;

                /*if(translatedMappedStrings.ContainsKey(ms.key))
                {

                    key = translatedMappedStrings[ms.key].key;
                    value = translatedMappedStrings[ms.key].value;
                }
                */
                Console.WriteLine("add to list " + key + " " + value);
                mappedstringlist.Add(new MappedString(key, value));
            }

            //add translated ones!
            foreach (mappedstringding ms in translatedMappedStrings)
            {
                string key = ms.key;
                string value = ms.value;

                Console.WriteLine("add to list " + key + " " + value);
                mappedstringlist.Add(new MappedString(key, value));
            }


            Console.WriteLine("reset stuffs");
            //reset mappedstringmanager
            MappedStringManager.getInstance().reset();
            //feed it with new mappedstrigns!
            MappedStringManager.getInstance().feed(mappedstringlist.ToArray());

            //reset the keywords

            MethodInfo generateKeywords = typeof(CardType).GetMethod("generateKeywords", BindingFlags.NonPublic | BindingFlags.Instance) ;
            foreach (CardType ct in cts)
            {
                generateKeywords.Invoke(ct, null);
            }

            //reset cardtypemanager
            CardTypeManager.getInstance().reset();
            //feed with edited cardtypes
            CardTypeManager.getInstance().feed(cts);

            CardType ctype =  CardTypeManager.getInstance().get("Plate Armor");
            Card c = new Card((long)123, ctype);

            Console.WriteLine("plate aromor:");
            foreach (KeywordDescription kd in c.getKeywords())
            {
                Console.WriteLine("" + kd.keyword + " : " + kd.description);
            }
            

        }

        private void setAchiveTexts()
        {
            List<AchievementType> alist = new List<AchievementType>( this.orginalAchivements.achievementTypes);

            List<AchievementType> addlist = new List<AchievementType>();

            foreach (AchievementType at in alist)
            {
                AchievementType nat = new AchievementType();


                //add other stuff from orginal
                nat.goldReward = at.goldReward;
                nat.group = at.group;
                nat.icon = at.icon;
                nat.id = at.id;
                nat.partType = at.partType;
                nat.sortId = at.sortId;
                nat.name = at.name;
                nat.description = at.description;

                if (this.achivementlist.ContainsKey(at.id.ToString()))
                {
                    achivementding ad = this.achivementlist[at.id.ToString()];

                    if (ad.oname == at.name && ad.odescription == at.description)
                    {
                        //change name + desc!
                        nat.name = ad.name;
                        nat.description = ad.description;
                        Console.WriteLine("### " + nat.name + " " + nat.description);
                    }
                    
                }

                addlist.Add(nat);
            }
            
            AchievementTypeManager.getInstance().reset();
            AchievementTypeManager.getInstance().feed(addlist.ToArray());
        }

        private void setOrginalCardtexts()
        {
            clearDictionaries();
            
            CardType[] cts = new CardType[this.orginalcards.cardTypes.Length];
            this.orginalcards.cardTypes.CopyTo(cts, 0);

            MappedString[] newmappedstrings = new MappedString[this.orginalmappedstrings.strings.Length];
            this.orginalmappedstrings.strings.CopyTo(newmappedstrings, 0);

            for (int i = 0; i < this.oid.Count; i++)
            {
                int cardid = Convert.ToInt32(this.oid[i]);//no need, but why not?:D
                string cardname = this.onames[i];
                string description = this.odesc[i];
                string flavor = this.oflavor[i];
                CardType.TypeSet ts = this.otypes[i];
                string[] aa = this.oactiveAbilitys[i];
                string[] pa = this.opassiveAbilitys[i];
                string[] pd = this.opassiveDescs[i];
                //get index from cts


                int ctsindex = getindexfromcardtypearray(cts, cardid);
                //change description
                if (ctsindex >= 0)
                {
                    cts[ctsindex].description = description;
                    //change flavor
                    cts[ctsindex].flavor = flavor;
                    cts[ctsindex].types = ts;
                    int j = 0;
                    foreach (ActiveAbility a in cts[ctsindex].abilities)
                    {
                        a.name = aa[j];
                        j++;
                    }
                    int k = 0;
                    foreach (PassiveAbility a in cts[ctsindex].passiveRules)
                    {
                        a.displayName = pa[k];
                        a.description = pd[k];
                        k++;
                    }

                }


            }

            //reset mapped strings
            List<MappedString> mappedstringlist = new List<MappedString>();
            foreach (MappedString ms in newmappedstrings)
            {
                string key = ms.key;
                string value = ms.value;
                mappedstringlist.Add(new MappedString(key, value));
            }

            
            Console.WriteLine("reset stuffs");
            //reset mappedstringmanager
            MappedStringManager.getInstance().reset();
            //feed it with new mappedstrigns!
            //MappedStringManager.getInstance().feed(mappedstringlist.ToArray());
            MappedStringManager.getInstance().feed(this.orginalmappedstrings.strings);

            //reset the keywords

            MethodInfo generateKeywords = typeof(CardType).GetMethod("generateKeywords", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (CardType ct in this.orginalcards.cardTypes) //cts)
            {
                generateKeywords.Invoke(ct, null);
            }

            //reset cardtypemanager
            CardTypeManager.getInstance().reset();
            //feed with edited cardtypes
            CardTypeManager.getInstance().feed(cts);

            //reset achivements
            AchievementTypeManager.getInstance().reset();
            AchievementTypeManager.getInstance().feed(this.orginalAchivements.achievementTypes);



        }

        private void clearDictionaries()
        {
            this.translatedPassiveAbility.Clear();
            this.translatedActiveAbility.Clear();
            this.translatedPieceType.Clear();
            this.translatedPieceKind.Clear();
            this.translatedPassiveDescription.Clear();
            this.translatedMappedStrings.Clear();
        }

        public void workthread()
        {
            Console.WriteLine("#workthread");
            string key = "ENG";

            string[] aucfiles = Directory.GetFiles(this.pathToConfig, "*.txt");
            string lol = "";
            if (aucfiles.Contains(this.pathToConfig + "Config.txt"))//File.Exists() was slower
            {
                lol = File.ReadAllText(this.pathToConfig + "Config.txt");
            }
            else
            {
                System.IO.File.WriteAllText(this.pathToConfig + "Config.txt", "ENG");
            }
            if (lol != "") { key = lol; }
            if (key == "RU") sttngs.usedLanguage = Language.RU;
            if (key == "ENG") sttngs.usedLanguage = Language.ENG;
            if (key == "DE") sttngs.usedLanguage = Language.DE;
            if (key == "FR") sttngs.usedLanguage = Language.FR;
            if (key == "SP") sttngs.usedLanguage = Language.SP;
           
            if (sttngs.usedLanguage == Language.RU) sttngs.usedFont = -1; // special for crylic
            if (key != "ENG")
            {
                this.setOrginalCardtexts();// or it would be possible to display multiple languages
                string response = this.getDataFromGoogleDocs(this.googlekeys[key]);
                Console.WriteLine("#response");
                Console.WriteLine(response);
                this.readJsonfromGoogle(response);
                this.setCardtexts();

                if (this.googleAchivementkeys.ContainsKey(key))
                {
                    response = this.getDataFromGoogleDocs(this.googleAchivementkeys[key]);
                    Console.WriteLine("#achivement response");
                    Console.WriteLine(response);
                    this.readAchiveJsonfromGoogle(response);
                    this.setAchiveTexts();
                }

                if (this.googleTutorialkeys.ContainsKey(key))
                {
                    response = this.getDataFromGoogleDocs(this.googleTutorialkeys[key]);
                    Console.WriteLine("#trial response");
                    Console.WriteLine(response);
                    this.readTrialJsonfromGoogle(response);
                }


            }
            else
            {
                setOrginalCardtexts();
            }
            RoomChatMessageMessage rcmm = new RoomChatMessageMessage();
            rcmm.from = "LanguageChanger";
            rcmm.roomName = this.chatroom;
            rcmm.text = "language was loaded!";

            if (this.notTranslatedScrolls)
            {
                rcmm.text = rcmm.text + "\r\n" + "list of untranslated cards:\r\n";
                foreach (string ntc in this.untranslatedcard)
                { 
                rcmm.text = rcmm.text + ntc + ", ";
                }

            }
            App.ArenaChat.handleMessage(rcmm);

        }

        public void incommingAchiveMessage(AchievementTypesMessage msg)
        {
            this.orginalAchivements = msg;
            new Thread(new ThreadStart(this.workthread)).Start(); 
        }

        public void incommingMappedStringsMessage(MappedStringsMessage msg)
        {
            this.orginalmappedstrings = msg;
            this.oMappedStrings.Clear();
            foreach (MappedString ms in msg.strings)
            {
                if (!this.oMappedStrings.ContainsKey(ms.key))
                {
                    this.oMappedStrings.Add(ms.key, ms.value);
                }
            }

            /*
            Console.WriteLine("refeed");
            MappedString[] newmappedstrings = new MappedString[msg.strings.Length];
            msg.strings.CopyTo(newmappedstrings, 0);
            newmappedstrings[0].value = "LoL";
            Console.WriteLine("change " + newmappedstrings[0].key + " to " + newmappedstrings[0].value);
            MappedStringManager.getInstance().reset();
            MappedStringManager.getInstance().feed(newmappedstrings);*/
        }

        public void incommingCardTypesMessage(CardTypesMessage msg)
        {
            clearDictionaries();
            this.orginalcards = msg;

            this.oid.Clear(); this.onames.Clear(); this.odesc.Clear(); this.oflavor.Clear();
            this.otypes.Clear(); this.oactiveAbilitys.Clear(); this.opassiveAbilitys.Clear();
            this.opassiveDescs.Clear();

            foreach (CardType ct in this.orginalcards.cardTypes) // have to copy the orginal values
            {
                oid.Add(ct.id.ToString());
                onames.Add(ct.name);
                odesc.Add(ct.description);
                oflavor.Add(ct.flavor);
                otypes.Add(new CardType.TypeSet(changePieceTypes(ct.types.ToString()).Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries)));

                string[] aa = new string[ct.abilities.Length];
                int i = 0;
                foreach (ActiveAbility a in ct.abilities)
                {
                    aa[i] = a.name;
                    i++;
                }
                this.oactiveAbilitys.Add(aa);

                string[] pa = new string[ct.passiveRules.Length];
                int j = 0;
                foreach (PassiveAbility a in ct.passiveRules)
                {
                    pa[j] = a.displayName;
                    j++;
                }
                this.opassiveAbilitys.Add(pa);

                string[] pd = new string[ct.passiveRules.Length];
                int k = 0;
                foreach (PassiveAbility a in ct.passiveRules)
                {
                    pd[k] = a.description;
                    k++;
                }
                this.opassiveDescs.Add(pd);

                /*foreach ( ActiveAbility aa in ct.abilities)
                {
                    Console.WriteLine("## Ability: "+aa.name);
                }*/
                /*foreach (PassiveAbility aa in ct.passiveRules)
                {
                    Console.WriteLine("## PassiveAbility: " + aa.displayName);
                }*/
            }
            
            //do this after achivements now :D
            //new Thread(new ThreadStart(this.workthread)).Start(); 
        }


        public void setTowerChallengeInfo(TowerChallengeInfo tci, GetTowerInfoMessage msg)
        {
            TowerLevel[] tls = msg.getSortedLevels();

            foreach (TowerLevel tl in tls)
            {
                if (this.triallist.ContainsKey(tl.id.ToString()))
                {
                    trialding td = this.triallist[tl.id.ToString()];
                    Console.WriteLine("### found trial " + tl.name + " " + tl.description);
                    string odesc = tl.description.Replace(" ", "");
                    odesc = odesc.Replace("\r\n", "");
                    odesc = odesc.Replace("\n", "");
                    odesc = odesc.Replace("\r", "");

                    string tdesc = td.odescription.Replace(" ","");
                    tdesc = tdesc.Replace("\r\n", "");
                    tdesc = tdesc.Replace("\r", "");
                    tdesc = tdesc.Replace("\n", "");

                    if (odesc != tdesc) //dont check the title!
                    {
                        Console.WriteLine("### dont match because of " + odesc + " " + tdesc);
                    }

                    string oflav = tl.flavour.Replace(" ", "");
                    oflav = oflav.Replace("\r\n", "");
                    oflav = oflav.Replace("\n", "");
                    oflav = oflav.Replace("\r", "");

                    string tflav = td.oflavour.Replace(" ", "");
                    tflav = tflav.Replace("\r\n", "");
                    tflav = tflav.Replace("\r", "");
                    tflav = tflav.Replace("\n", "");

                    if (oflav != tflav) //dont check the title!
                    {
                        Console.WriteLine("### dont match because of " + oflav + " " + tflav);
                    }

                    string oname = tl.name.Replace(" ", "");
                    oname = oname.Replace("\r\n", "");
                    oname = oname.Replace("\n", "");
                    oname = oname.Replace("\r", "");

                    string tname = td.oname.Replace(" ", "");
                    tname = tname.Replace("\r\n", "");
                    tname = tname.Replace("\r", "");
                    tname = tname.Replace("\n", "");

                    if (oname != tname) //dont check the title!
                    {
                        Console.WriteLine("### dont match because of " + oname + " " + tname);
                    }

                    if (odesc == tdesc && oflav == tflav && oname == tname) //dont check the title!
                    {
                        tl.description = td.description;
                        tl.name = td.name;
                        tl.flavour = td.flavour;
                        tl.title = td.title;
                        
                    }
                }
            }
            tci.setLevels(tls);
        }

    }
}
