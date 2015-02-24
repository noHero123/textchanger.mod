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


namespace TranslationTool.mod
{
    public class textchanger : BaseMod, ICommListener
	{

        CardTextTranslator ctt;
        Settings sttngs;
        private string pathToConfig = "";
        FieldInfo textsArrField;
        FieldInfo helpOverlayField;
        FieldInfo renderQueueOffsetsfield;

        public void handleMessage(Message msg)
        { // collect data for enchantments (or units who buff)

            if (msg is RoomChatMessageMessage)
            {
                RoomChatMessageMessage rcmm = (RoomChatMessageMessage)msg;
                if (rcmm.text.StartsWith("You have joined"))
                {

                    RoomChatMessageMessage nrcmm = new RoomChatMessageMessage(rcmm.roomName, "Change the language of the card-descriptions with /language ENG (for example).\r\nto change the font type /language font arial\r\nfor a list of available languages/fonts type /language help");
                        nrcmm.from = "LanguageChanger";

                        ctt.chatroom = nrcmm.roomName;

                        App.ArenaChat.handleMessage(nrcmm);
                        App.Communicator.removeListener(this);

                }
            }

            if (msg is MappedStringsMessage)
            {
                MappedStringsMessage msm = (MappedStringsMessage)msg;
                ctt.incommingMappedStringsMessage(msm);
            }

            if (msg is AchievementTypesMessage)
            {
                AchievementTypesMessage msm = (AchievementTypesMessage)msg;
                ctt.incommingAchiveMessage(msm);
            }

            
            /*if (msg is NewEffectsMessage )
            {
               //for translating the tutorial chat-effect-messages!
                List<EffectMessage> effects = NewEffectsMessage.parseEffects(msg.getRawText());
            }*/

            return;
        }


        public void onConnect(OnConnectData ocd)
        {
            return; // don't care
        }


		//initialize everything here, Game is loaded at this point
        public textchanger()
		{
            this.textsArrField = typeof(CardView).GetField("textsArr", BindingFlags.Instance | BindingFlags.NonPublic);
            this.helpOverlayField = typeof(CardView).GetField("helpOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
            this.renderQueueOffsetsfield = typeof(CardView).GetField("renderQueueOffsets", BindingFlags.Instance | BindingFlags.NonPublic);
            sttngs = new Settings();
            this.pathToConfig = this.OwnFolder() + System.IO.Path.DirectorySeparatorChar;
            ctt = new CardTextTranslator(pathToConfig, sttngs);
            ctt.googlekeys.Add("DE", "0AhhxijYPL-BGdDJaWTI4UVJ3OUZfYzlCSWo3dkZSOXc");
            ctt.googlekeys.Add("FR", "0AsfEX06xqzPEdE9lQlg5NFg2ejBRamltMEhta2FrX2c");
            //ctt.googlekeys.Add("RU", "0AsYOnt3MiugydDRRUEp4eXU0VUloYUxiSW5nVXl0Y1E");//old one
            ctt.googlekeys.Add("RU", "19I6vAusLM-iDWYAh9c7NL27VHjamoV4ZCw-4L55kLqw");
            ctt.googlekeys.Add("SP", "0AprX3iUTAgX9dDcyUUhQSnVndkxCSjVXTzJ6NDA0c3c");


            
            ctt.googleAchivementkeys.Add("RU", "1LVU7ZzOW_oK12va2SoHzp3XfLLFxEOvB5gaZ6OBdRDI");



            ctt.googleTutorialkeys.Add("RU", "14-F9bQMXBdEn_7est4Xtc_b9lajnwdBHJiSqHE5P58w");



           


            try
            {
                App.Communicator.addListener(this);
            }
            catch { }

            return;

            //only for tests###################################

            ctt.googlekeys.Add("test", "1zeYq6pCk8R1jc18mBFucQHsTTAnDMe0F56jqwOl4vU0");//test ding!
            ctt.googleAchivementkeys.Add("test", "1WD7NMAXOJUcn3mm5-ZCQj7nynlvjVug8ZWetHWuTFuU");//test ding!
            ctt.googleTutorialkeys.Add("test", "10KQhaApAQCOhKARzXeY6_T-xUYyC3AiPoj2710d6M7o");//test ding!


            int k = 0;
            int l = 0;
            DateTime itze = DateTime.Now;
            for (int i = 0; i < 1; i++)
            {
                string ttx = this.getDataFromGoogleDocs(ctt.googlekeys["test"]);
                this.getDataFromGoogleDocs(ctt.googleAchivementkeys["test"]);
                this.getDataFromGoogleDocs(ctt.googleTutorialkeys["test"]);
                //Console.WriteLine(ttx);
                ctt.readJsonfromGoogleFast(ttx);
                k++;
            }
            DateTime itze1 = DateTime.Now;

            //Console.WriteLine("#####");
            
            for (int i = 0; i < 1; i++)
            {
                string txt = this.getDataFastFromGoogleDocs(ctt.googlekeys["test"]);
                this.getDataFastFromGoogleDocs(ctt.googleAchivementkeys["test"]);
                this.getDataFastFromGoogleDocs(ctt.googleTutorialkeys["test"]);
                //Console.WriteLine(txt);
                ctt.readJsonfromGoogleFast(txt);
                l++;
            }
            DateTime itze2 = DateTime.Now;


            Console.WriteLine("### "+((itze1 - itze).TotalSeconds/k) + " vs " + ((itze2 - itze1).TotalSeconds/l));
		}



        //teststuff json vs txt with 10 trys 
        public string getDataFromGoogleDocs(string googledatakey)
        {
            WebRequest myWebRequest;

            //https://docs.google.com/spreadsheet/pub?key=0AhhxijYPL-BGdDJaWTI4UVJ3OUZfYzlCSWo3dkZSOXc&output=txt

            myWebRequest = WebRequest.Create("https://spreadsheets.google.com/feeds/list/" + googledatakey + "/od6/public/values?alt=json");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            myWebRequest.Timeout = 10000;


            int loaded = 0;
            string ressi = "";
            while (loaded < 10)
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
                    Console.WriteLine("to");
                }
            }
            
            return ressi;
        }

        public string getDataFastFromGoogleDocs(string googledatakey)
        {
            WebRequest myWebRequest;

            //https://docs.google.com/spreadsheets/d/1WD7NMAXOJUcn3mm5-ZCQj7nynlvjVug8ZWetHWuTFuU/export?format=tsv&id=1WD7NMAXOJUcn3mm5-ZCQj7nynlvjVug8ZWetHWuTFuU&gid=0
            //new sheets:
            myWebRequest = WebRequest.Create("https://docs.google.com/spreadsheets/d/" + googledatakey + "/export?format=tsv&id=" + googledatakey + "&gid=0");
            //Console.WriteLine("https://docs.google.com/spreadsheets/d/" + googledatakey + "/export?format=tsv&id=" + googledatakey + "&gid=0");
            
            //old sheets:
            //myWebRequest = WebRequest.Create("https://docs.google.com/spreadsheet/pub?key=" + googledatakey + "&output=txt");
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => true;// or you get an exeption, because mono doesnt trust anyone
            myWebRequest.Timeout = 10000;

            int loaded = 0;
            string ressi = "";
            while (loaded < 10)
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
                    Console.WriteLine("tof");
                }
            }

            return ressi;
        }




		public static string GetName ()
		{
			return "textchanger";
		}

		public static int GetVersion ()
		{
			return 22;
		}


       
		//only return MethodDefinitions you obtained through the scrollsTypes object
		//safety first! surround with try/catch and return an empty array in case it fails
		public static MethodDefinition[] GetHooks (TypeDefinitionCollection scrollsTypes, int version)
		{
            try
            {
                return new MethodDefinition[] {

                    scrollsTypes["GlobalMessageHandler"].Methods.GetMethod("handleMessage",new Type[]{typeof(CardTypesMessage)}),
                    scrollsTypes["Communicator"].Methods.GetMethod("send", new Type[]{typeof(Message)}),
                    scrollsTypes["CardView"].Methods.GetMethod("createText_PassiveAbilities")[0], // for changeing the font 
                    //scrollsTypes["CardView"].Methods.GetMethod("createHelpOverlay")[0], // for changeing the font
           
                    scrollsTypes["CardView"].Methods.GetMethod("createText",new Type[]{typeof(GameObject), typeof(Font), typeof(string), typeof(int), typeof(float), typeof(Vector3)}),

                    scrollsTypes["TowerChallengeInfo"].Methods.GetMethod("SetTowerChallengeInfo",new Type[]{typeof(GetTowerInfoMessage)}),

                    //scrollsTypes["Card"].Methods.GetMethod("getPieceKindText")[0], // to slow
             };
            }
            catch
            {
                return new MethodDefinition[] { };
            }
		}

      


        public override bool WantsToReplace(InvocationInfo info)
        {
            /*if (info.target is Card && info.targetMethod.Equals("getPieceKindText"))
            { return true; } */
            if (info.target is Communicator && info.targetMethod.Equals("send") && info.arguments[0] is RoomChatMessageMessage)
            {
                if (((RoomChatMessageMessage)info.arguments[0]).text.StartsWith("/language "))
                {
                    Console.WriteLine("##sendRequest");
                    return true;
                }
            }
            if (info.target is CardView && info.targetMethod.Equals("createText"))//createTexts
            {
                string name = (string)(info.arguments[2]);
                if (name.StartsWith("keyword_") || name.StartsWith("description_"))
                {
                    return true;
                }
            }

            if (info.target is TowerChallengeInfo && info.targetMethod.Equals("SetTowerChallengeInfo") && sttngs.usedLanguage != Language.ENG)
            {
                return true;
            }

            return false;
        }


        private GameObject createText(GameObject parent, Font font, string name, int fontSize, float characterSize, Vector3 scale, CardView cv)
        {
            if (sttngs.usedLanguage == Language.RU)
            {
                fontSize = (int)(fontSize * 0.725);
            }

            if (sttngs.usedFont == -1 && sttngs.usedLanguage == Language.RU)
            {
                font = (Font)ResourceManager.Load("Fonts/arial"); 
            }
            if (sttngs.usedFont == 1) { font = (Font)ResourceManager.Load("Fonts/arial"); }
            else
            {
                if (sttngs.usedFont == 2) { font = (Font)ResourceManager.Load("Fonts/HoneyMeadBB_bold"); }
                else
                {
                    if (sttngs.usedFont == 3) { font = (Font)ResourceManager.Load("Fonts/HoneyMeadBB_boldital"); }
                    else
                    {
                        if (sttngs.usedFont == 4) { font = (Font)ResourceManager.Load("Fonts/dwarvenaxebb"); }
                    }
                }
            }

            GameObject gameObject = new GameObject("TextMesh");
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            TextMesh textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.font = font;
            
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            textMesh.lineSpacing = 0.85f;
            gameObject.name = name;
            font.material.color = Color.black;
            meshRenderer.material = font.material;
            meshRenderer.material.shader = ResourceManager.LoadShader("Scrolls/GUI/3DTextShader");//CardView.fontShader

            //this.renderQueueOffsets.Add(meshRenderer, -4);
            ((Dictionary<Renderer, int>)this.renderQueueOffsetsfield.GetValue(cv)).Add(meshRenderer, -4);

            UnityUtil.addChild(parent, gameObject);
            gameObject.transform.localScale = scale;
            gameObject.transform.localEulerAngles = new Vector3(90f, 90f, 270f);
            gameObject.renderer.material.color = new Color(0.23f, 0.16f, 0.125f);
            return gameObject;
        }


        public override void ReplaceMethod(InvocationInfo info, out object returnValue)
        {
            returnValue = null;
            /*
            if (info.target is Card && info.targetMethod.Equals("getPieceKindText"))
            {
                string retu = (info.target as Card).getPieceKind().ToString();
                if (this.translatedPieceKind.ContainsKey(retu))
                { retu = translatedPieceKind[retu]; }
                returnValue = retu;
                if ((info.target as Card).isToken)
                {
                    returnValue = "TOKEN " + returnValue;
                }

            }*/
            if (info.target is CardView && info.targetMethod.Equals("createText"))//createTexts
            {
                returnValue = createText((GameObject)info.arguments[0], (Font)info.arguments[1], (string)info.arguments[2], (int)info.arguments[3], (float)info.arguments[4], (Vector3)info.arguments[5], (CardView)info.target);
            }

            if(info.target is TowerChallengeInfo && info.targetMethod.Equals("SetTowerChallengeInfo"))
            {
                ctt.setTowerChallengeInfo(info.target as TowerChallengeInfo, info.arguments[0] as GetTowerInfoMessage);
                returnValue = true;
            }
            
            if (info.target is Communicator && info.targetMethod.Equals("send") && info.arguments[0] is RoomChatMessageMessage && (info.arguments[0] as RoomChatMessageMessage).text.StartsWith("/language "))
            {
                RoomChatMessageMessage rcmm = info.arguments[0] as RoomChatMessageMessage;
                rcmm.from = "LanguageChanger";

                // CHANGE FONT
                if ((info.arguments[0] as RoomChatMessageMessage).text.StartsWith("/language font"))
                {

                    string choosenFont = rcmm.text.Replace("/language font ", "");
                    if (choosenFont == "arial")
                    {
                        sttngs.usedFont = 1;
                        rcmm.text ="Font was changed to arial";
                    }
                    else
                    {
                        if (choosenFont == "honey1")
                        {
                            sttngs.usedFont = 2;
                            rcmm.text = "Font was changed to honey1";
                        }
                        else
                        {
                            if (choosenFont == "honey2")
                            {
                                sttngs.usedFont = 3;
                                rcmm.text = "Font was changed to honey2";
                            }
                            else
                            {
                                if (choosenFont == "dwar")
                                {
                                    sttngs.usedFont = 4;
                                    rcmm.text = "Font was changed to dwar";
                                }
                                else
                                {
                                    sttngs.usedFont = 0;
                                    rcmm.text = "Font was changed to default";
                                }
                            }
                        }
                    }
                    App.ArenaChat.handleMessage(rcmm);
                    returnValue = true;
                    return;
                }

                // CHANGE LANGUAGE
                Console.WriteLine("##start");
                string language = rcmm.text.Replace("/language ", "");
                if (ctt.googlekeys.ContainsKey(language) || language == "ENG")
                {
                    System.IO.File.WriteAllText(this.pathToConfig + "Config.txt", language);
                    ctt.notTranslatedScrolls = false;
                    new Thread(new ThreadStart(ctt.workthread)).Start();
                    rcmm.text = "the language is going to changed to " + language +", please wait...";

                    if (language == "RU") sttngs.usedFont = -1; // special for crylic

                }
                else
                {
                    string available = "ENG";
                    foreach (string key in ctt.googlekeys.Keys)
                    { available = available + " "+ key; }
                    rcmm.text = "available languages are: " +  available;
                    rcmm.text = rcmm.text + "\r\navailable fonts are: default, arial, honey1, honey2, dwar";
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

        public override void AfterInvoke (InvocationInfo info, ref object returnValue)
        {
            //Console.WriteLine("######" + info.targetMethod);
            if (info.target is Card)//createTexts
            {
                Console.WriteLine("#######" + ((Card)info.target).getName());
                Console.WriteLine("#######" + info.stackTrace);
            }
            if (info.target is GlobalMessageHandler && info.targetMethod.Equals("handleMessage") )
            {
                if (info.arguments[0] is CardTypesMessage)
                {
                    CardTypesMessage msg = (CardTypesMessage)info.arguments[0];
                    ctt.incommingCardTypesMessage(msg);
                }

            }



            if (info.target is CardView && info.targetMethod.Equals("createHelpOverlay"))//createTexts
            {
                if (sttngs.usedFont == 0 && sttngs.usedLanguage == Language.RU)
                {
                    GameObject image = (GameObject)this.helpOverlayField.GetValue(info.target);
                    
                    if (image != null && image.name == "help_overlay")
                    {
                        try
                        {
                            TextMesh[] lol3 = image.GetComponentsInChildren<TextMesh>();
                            foreach (TextMesh lol1 in lol3)
                            {

                                if (lol1.name != "3DText_title")
                                {
                                    lol1.fontSize = (int)(lol1.fontSize * 0.725);
                                }
                            }

                            //TextMesh lol2 = image.GetComponentInChildren<TextMesh>();
                            //lol2.fontSize = (int)(lol2.fontSize * 0.725);
                        }
                        catch
                        {
                            Console.WriteLine("#error in changesize");
                        }
                    }
                }

                if (sttngs.usedFont == -1 && sttngs.usedLanguage == Language.RU)
                {
                    // change the font/size/alingment

                    Font ffont = (Font)ResourceManager.Load("Fonts/arial", typeof(Font));

                    GameObject image = (GameObject)this.helpOverlayField.GetValue(info.target);
                    if (image != null)
                    {
                        try
                        {
                            TextMesh[] lol3 = image.GetComponentsInChildren<TextMesh>();
                            foreach (TextMesh lol1 in lol3)
                            {
                                if (lol1.name != "3DText_title")
                                {
                                    
                                    Color c2 = image.renderer.material.color;
                                    int fsize = lol1.fontSize;
                                    float csize = lol1.characterSize;
                                    lol1.font = ffont;
                                    lol1.fontSize = fsize;
                                    lol1.characterSize = csize;
                                    ffont.material.color = Color.black;
                                    lol1.gameObject.renderer.material = ffont.material;
                                    lol1.gameObject.renderer.material.color = new Color(0.23f, 0.16f, 0.125f);
                                    //lol1.gameObject.GetComponent<MeshRenderer>().material = ffont.material;
                                    //lol1.gameObject.GetComponent<MeshRenderer>().material.shader = ResourceManager.LoadShader("Scrolls/GUI/3DTextShader");

                                    
                                    lol1.fontSize = (int)(lol1.fontSize * 0.725);
                                }
                            }

                        /*
                        TextMesh lol2 = image.GetComponentInChildren<TextMesh>();
                        Color c2 = image.renderer.material.color;
                        lol2.font = ffont;
                        image.renderer.material = ffont.material;
                        image.renderer.material.color = c2;
                        lol2.fontSize = (int)(lol2.fontSize * 0.725);
                        */

                        }
                        catch
                        {
                            Console.WriteLine("#error in changesize");
                        }
                    }

                }

            }


            // change font of card
            if (info.target is CardView && info.targetMethod.Equals("createText_PassiveAbilities"))//createTexts
            {
                //some exceptions for russian language (crylic is stupid ;_;)
                if (sttngs.usedFont == 0 && sttngs.usedLanguage == Language.RU)
                { 
                    List<GameObject> Images = (List<GameObject>)this.textsArrField.GetValue(info.target);
                    foreach (GameObject go in Images)
                    {
                        try
                        {
                            if (go.name != "3DText_title" && go.name != "3DText_pieceType")
                            {
                                TextMesh lol = go.GetComponentInChildren<TextMesh>();
                                //lol.anchor = TextAnchor.MiddleCenter;
                                lol.fontSize = (int)(lol.fontSize * 0.725);
                            }
                        }
                        catch { }
                    }

                    

                    return;
                }

                if (sttngs.usedFont == -1 && sttngs.usedLanguage == Language.RU)
                {
                    // change the font/size/alingment
                    List<GameObject> Images = (List<GameObject>)this.textsArrField.GetValue(info.target);
                    Font ffont = (Font)ResourceManager.Load("Fonts/arial", typeof(Font));
                    for (int i = Images.Count-1; i>=0; i--)
                    {
                        GameObject go = Images[i];
                        try
                        {
                            //Console.WriteLine("## goname " + go.name);
                        
                            TextMesh lol = go.GetComponentInChildren<TextMesh>();
                            //lol.anchor = TextAnchor.MiddleCenter;
                            if (go.name != "3DText_title" && go.name != "3DText_pieceType")
                            {
                                //ffont.material.color = go.renderer.material.color;
                                Color c = go.renderer.material.color;
                                lol.font = ffont;
                                go.renderer.material = ffont.material;
                                go.renderer.material.color = c;
                                lol.fontSize = (int)(lol.fontSize * 0.725);
                            }
                        }
                        catch { }
                    }

                    return;
                }



                if (sttngs.usedFont >= 1)
                {
                    //Console.WriteLine("change font");
                    Font ffont = (Font)ResourceManager.Load("Fonts/arial", typeof(Font));
                    //some fonts I found in scrolls
                    if (sttngs.usedFont == 1) { ffont = (Font)ResourceManager.Load("Fonts/arial", typeof(Font)); }
                    if (sttngs.usedFont == 2) { ffont = (Font)ResourceManager.Load("Fonts/HoneyMeadBB_bold", typeof(Font)); }
                    if (sttngs.usedFont == 3) { ffont = (Font)ResourceManager.Load("Fonts/HoneyMeadBB_boldital", typeof(Font)); }
                    if (sttngs.usedFont == 4) { ffont = (Font)ResourceManager.Load("Fonts/dwarvenaxebb", typeof(Font)); }

                    // change the font/size/alingment
                    List<GameObject> Images = (List<GameObject>)this.textsArrField.GetValue(info.target);
                    //ffont.material.color = Color.blue;
                    foreach (GameObject go in Images)
                    {
                        try
                        {
                            TextMesh lol = go.GetComponentInChildren<TextMesh>();
                            Color c = go.renderer.material.color;
                            lol.font = ffont;
                            go.renderer.material = ffont.material;
                            go.renderer.material.color = c;
                            if (sttngs.usedFont == 1) lol.fontSize = (int)(lol.fontSize * 0.725);
                        }
                        catch { }
                    }
                }
                

            }
         

            //returnValue = null;
            return;//return false;
        }



        
	}
}
