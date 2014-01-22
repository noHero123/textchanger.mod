using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonFx.Json;
using System.Threading;
using System.Net;
using System.IO;

namespace textchanger.mod
{
    class CardTextTranslator
    {
        private string pathToConfig = "";
        public Dictionary<string, string> googlekeys = new Dictionary<string, string>();
        CardTypesMessage orginalcards;
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
        List<CardType.TypeSet> otypes = new List<CardType.TypeSet>();
        List<string[]> oactiveAbilitys = new List<string[]>();
        List<string[]> opassiveAbilitys = new List<string[]>();
        Dictionary<string, string> translatedPieceKind = new Dictionary<string, string>();
        Dictionary<string, string> translatedActiveAbility = new Dictionary<string, string>();
        Dictionary<string, string> translatedPieceType = new Dictionary<string, string>();
        Dictionary<string, string> translatedPassiveAbility = new Dictionary<string, string>();

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


        private void setCardtexts()
        {
            clearDictionaries();

            CardType[] cts = new CardType[this.orginalcards.cardTypes.Length];
            this.orginalcards.cardTypes.CopyTo(cts, 0);
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

                int ctsindex = getindexfromcardtypearray(cts, cardid);
                //change description
                if (ctsindex >= 0)
                {
                    if (transDesc == cts[ctsindex].description)
                    { cts[ctsindex].description = description; }
                    else
                    {
                        Console.WriteLine("## " + cardname + " description was changed, so it is not translated");
                        this.notTranslatedScrolls = true;
                    }
                    //change flavor
                    cts[ctsindex].flavor = flavor;
                }




            }

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
                    }
                }
            }



            //reset cardtypemanager
            CardTypeManager.getInstance().reset();
            //feed with edited cardtypes
            CardTypeManager.getInstance().feed(cts);

        }

        private void setOrginalCardtexts()
        {
            clearDictionaries();

            CardType[] cts = new CardType[this.orginalcards.cardTypes.Length];
            this.orginalcards.cardTypes.CopyTo(cts, 0);
            for (int i = 0; i < this.oid.Count; i++)
            {
                int cardid = Convert.ToInt32(this.oid[i]);//no need, but why not?:D
                string cardname = this.onames[i];
                string description = this.odesc[i];
                string flavor = this.oflavor[i];
                CardType.TypeSet ts = this.otypes[i];
                string[] aa = this.oactiveAbilitys[i];
                string[] pa = this.opassiveAbilitys[i];
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
                        k++;
                    }
                }


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
            sttngs.usedLanguage = key;
            if (sttngs.usedLanguage == "RU") sttngs.usedFont = -1; // special for crylic
            if (key != "ENG")
            {
                this.setOrginalCardtexts();// or it would be possible to display multiple languages
                string response = this.getDataFromGoogleDocs(this.googlekeys[key]);
                this.readJsonfromGoogle(response);
                this.setCardtexts();
            }
            else
            {
                setOrginalCardtexts();
            }

        }


        public void incommingCardTypesMessage(CardTypesMessage msg)
        {
            clearDictionaries();
            this.orginalcards = msg;
            this.oid.Clear(); this.onames.Clear(); this.odesc.Clear(); this.oflavor.Clear();
            this.otypes.Clear(); this.oactiveAbilitys.Clear(); this.opassiveAbilitys.Clear();
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
