using System;

using ScrollsModLoader.Interfaces;
using UnityEngine;
using Mono.Cecil;
//using Mono.Cecil;
//using ScrollsModLoader.Interfaces;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;
using JsonFx.Json;
using System.Text.RegularExpressions;
using System.Threading;


namespace textchanger.mod
{
    public class textchanger : BaseMod, ICommListener
	{
        Dictionary<string, string> googlekeys = new Dictionary<string, string>();
        CardTypesMessage orginalcards;
        List<string> id = new List<string>();
        List<string> names = new List<string>();
        List<string> desc = new List<string>();
        List<string> flavor = new List<string>();
        List<string> oid = new List<string>();
        List<string> onames = new List<string>();
        List<string> odesc = new List<string>();
        List<string> oflavor = new List<string>();
        private string pathToConfig = "";


        public void handleMessage(Message msg)
        { // collect data for enchantments (or units who buff)

            if (msg is RoomChatMessageMessage)
            {
                RoomChatMessageMessage rcmm = (RoomChatMessageMessage)msg;
                if (rcmm.text.StartsWith("You have joined"))
                {

                        RoomChatMessageMessage nrcmm = new RoomChatMessageMessage(rcmm.roomName, "Change the language of the card-descriptions with /language ENG (for example).\r\nfor a list of available languages type /language help");
                        nrcmm.from = "LanguageChanger";
                        App.ArenaChat.handleMessage(nrcmm);
                        App.Communicator.removeListener(this);

                }
            }

            return;
        }


        public void onConnect(OnConnectData ocd)
        {
            return; // don't care
        }


		//initialize everything here, Game is loaded at this point
        public textchanger()
		{
            this.pathToConfig = this.OwnFolder() + System.IO.Path.DirectorySeparatorChar;
            googlekeys.Add("DE", "0AhhxijYPL-BGdDJaWTI4UVJ3OUZfYzlCSWo3dkZSOXc");
            googlekeys.Add("FR", "0AsfEX06xqzPEdE9lQlg5NFg2ejBRamltMEhta2FrX2c");
            //descriptionField = typeof(CardType).GetField("description", BindingFlags.Instance | BindingFlags.NonPublic);
            //flavorField = typeof(CardType).GetField("flavor", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                App.Communicator.addListener(this);
            }
            catch { }
		}

        

		public static string GetName ()
		{
			return "textchanger";
		}

		public static int GetVersion ()
		{
			return 1;
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

        private void readJsonfromGoogle(string txt)
        {
            //Console.WriteLine(txt);
            this.names.Clear();
            this.desc.Clear();
            this.flavor.Clear();
            this.id.Clear();
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
                    
                }

            }

           
        }



		//only return MethodDefinitions you obtained through the scrollsTypes object
		//safety first! surround with try/catch and return an empty array in case it fails
		public static MethodDefinition[] GetHooks (TypeDefinitionCollection scrollsTypes, int version)
		{
            try
            {
                return new MethodDefinition[] {
                    scrollsTypes["GlobalMessageHandler"].Methods.GetMethod("handleMessage",new Type[]{typeof(CardTypesMessage)}),
                    scrollsTypes["Communicator"].Methods.GetMethod("sendRequest", new Type[]{typeof(Message)}), 
             };
            }
            catch
            {
                return new MethodDefinition[] { };
            }
		}


        public override bool WantsToReplace(InvocationInfo info)
        {
            if (info.target is Communicator && info.targetMethod.Equals("sendRequest") && info.arguments[0] is RoomChatMessageMessage && (info.arguments[0] as RoomChatMessageMessage).text.StartsWith("/language "))
            {
                Console.WriteLine("##sendRequest");
                return true;
            }

            return false;
        }

        public override void ReplaceMethod(InvocationInfo info, out object returnValue)
        {
            returnValue = null;
            if (info.target is Communicator && info.targetMethod.Equals("sendRequest") && info.arguments[0] is RoomChatMessageMessage && (info.arguments[0] as RoomChatMessageMessage).text.StartsWith("/language "))
            {
                Console.WriteLine("##start");
                RoomChatMessageMessage rcmm= info.arguments[0] as RoomChatMessageMessage;
                rcmm.from = "LanguageChanger";
                string language = rcmm.text.Replace("/language ", "");
                if (this.googlekeys.ContainsKey(language) || language == "ENG")
                {
                    System.IO.File.WriteAllText(this.pathToConfig + "Config.txt", language);
                    new Thread(new ThreadStart(this.workthread)).Start();
                    rcmm.text = "the language was changed to " + language;
                }
                else
                {
                    string available = "ENG";
                    foreach (string key in this.googlekeys.Keys)
                    { available = available + " "+ key; }
                    rcmm.text = "available languages are: " +  available;
                }
                App.ArenaChat.handleMessage(rcmm);
                returnValue = true;
                return;
            }
            
        }

        public override void BeforeInvoke(InvocationInfo info)
        {

            return;

        }

        private int getindexfromcardtypearray(CardType[] cts, string cardname)
        {
            int retval = -1;

            for (int i = 0; i <= cts.Length - 1; i++)
            {
                if (cts[i].name.ToLower() == cardname.ToLower())
                {
                    retval = i;
                    break; // break passt schon :P
                }
            }
            return retval;
        }

        private void setCardtexts()
        {
            CardType[] cts = new CardType[this.orginalcards.cardTypes.Length];
            this.orginalcards.cardTypes.CopyTo(cts,0);
            for (int i = 0; i < this.id.Count;i++ )
            {
                int cardid = Convert.ToInt32(this.id[i]);//no need, but why not?:D
                string cardname = this.names[i];
                string description = this.desc[i];
                string flavor = this.flavor[i];

                //get index from cts
                int ctsindex = getindexfromcardtypearray(cts, cardname);
                //change description
                if (ctsindex >= 0)
                {
                    cts[ctsindex].description = description;
                    //change flavor
                    cts[ctsindex].flavor = flavor;
                }

            }
            //reset cardtypemanager
            CardTypeManager.getInstance().reset();
            //feed with edited cardtypes
            CardTypeManager.getInstance().feed(cts);

        }

        private void setOrginalCardtexts()
        {
            CardType[] cts = new CardType[this.orginalcards.cardTypes.Length];
            this.orginalcards.cardTypes.CopyTo(cts, 0);
            for (int i = 0; i < this.id.Count; i++)
            {
                int cardid = Convert.ToInt32(this.oid[i]);//no need, but why not?:D
                string cardname = this.onames[i];
                string description = this.odesc[i];
                string flavor = this.oflavor[i];

                //get index from cts
                int ctsindex = getindexfromcardtypearray(cts, cardname);
                //change description
                if (ctsindex >= 0)
                {
                    cts[ctsindex].description = description;
                    //change flavor
                    cts[ctsindex].flavor = flavor;
                }

            }
            //reset cardtypemanager
            CardTypeManager.getInstance().reset();
            //feed with edited cardtypes
            CardTypeManager.getInstance().feed(cts);

        }



        public void workthread()
        {
            Console.WriteLine("#workthread");
            string key = "ENG";

            string [] aucfiles = Directory.GetFiles(this.pathToConfig, "*.txt");
            string lol = "";
            if (aucfiles.Contains(this.pathToConfig + "Config.txt"))//File.Exists() was slower
            {
                lol = File.ReadAllText(this.pathToConfig + "Config.txt");
            }
            else 
            {
                System.IO.File.WriteAllText(this.pathToConfig + "Config.txt", "ENG");
            }
            if (lol != ""){ key = lol; }
            if (key != "ENG")
            {
                this.setOrginalCardtexts(); // or it would be possible to have multiple languages displayed
                string response = this.getDataFromGoogleDocs(this.googlekeys[key]);
                this.readJsonfromGoogle(response);
                this.setCardtexts();
            }
            else
            {
                setOrginalCardtexts();
            }
            
        }

        public override void AfterInvoke (InvocationInfo info, ref object returnValue)
        //public override bool BeforeInvoke(InvocationInfo info, out object returnValue)
        {
            if (info.target is GlobalMessageHandler && info.targetMethod.Equals("handleMessage") && info.arguments[0] is CardTypesMessage)
            {
                CardTypesMessage msg = (CardTypesMessage)info.arguments[0];
                this.orginalcards = msg;
                this.oid.Clear(); this.onames.Clear(); this.odesc.Clear(); this.oflavor.Clear();
                foreach (CardType ct in this.orginalcards.cardTypes) // have to copy the orginal values
                {
                    oid.Add(ct.id.ToString());
                    onames.Add(ct.name);
                    odesc.Add(ct.description);
                    oflavor.Add(ct.flavor);
                }
                new Thread(new ThreadStart(this.workthread)).Start();
            }

           
         

            //returnValue = null;
            return;//return false;
        }



        
	}
}

