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

    class CardTextTranslator
    {
        private string pathToConfig = "";
        public Dictionary<string, string> googlekeys = new Dictionary<string, string>();
        CardTypesMessage orginalcards;
        MappedStringsMessage orginalmappedstrings;

        List<string> id = new List<string>();
        List<string> names = new List<string>();
        List<string> desc = new List<string>();
        List<string> flavor = new List<string>();
         List<string> translatedDesc = new List<string>();
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

        private string getDataFromGoogleDocs(string googledatakey)
        {
            WebRequest myWebRequest;
            myWebRequest = WebRequest.Create("https://spreadsheets.google.com/feeds/list/" + googledatakey + "/od6/public/values?alt=json");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            myWebRequest.Timeout = 10000;
            WebResponse myWebResponse = myWebRequest.GetResponse();
            System.IO.Stream stream = myWebResponse.GetResponseStream();
            System.IO.StreamReader reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
            string ressi = reader.ReadToEnd();
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
                        Console.WriteLine("add mapped string " + cardname + " " + description + " " + flavor);
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
            MappedStringManager.getInstance().feed(mappedstringlist.ToArray());

            //reset the keywords

            MethodInfo generateKeywords = typeof(CardType).GetMethod("generateKeywords", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (CardType ct in cts)
            {
                generateKeywords.Invoke(ct, null);
            }

            //reset cardtypemanager
            CardTypeManager.getInstance().reset();
            //feed with edited cardtypes
            CardTypeManager.getInstance().feed(cts);



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
            }
            else
            {
                setOrginalCardtexts();
            }

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
            new Thread(new ThreadStart(this.workthread)).Start();
        }

    }
}
