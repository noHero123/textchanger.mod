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

        CardTextTranslator ctt;
        Settings sttngs;
        private string pathToConfig = "";
        FieldInfo textsArrField;

        public void handleMessage(Message msg)
        { // collect data for enchantments (or units who buff)

            if (msg is RoomChatMessageMessage)
            {
                RoomChatMessageMessage rcmm = (RoomChatMessageMessage)msg;
                if (rcmm.text.StartsWith("You have joined"))
                {

                    RoomChatMessageMessage nrcmm = new RoomChatMessageMessage(rcmm.roomName, "Change the language of the card-descriptions with /language ENG (for example).\r\nto change the font type /language font arial\r\nfor a list of available languages/fonts type /language help");
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
            this.textsArrField = typeof(CardView).GetField("textsArr", BindingFlags.Instance | BindingFlags.NonPublic);
            sttngs = new Settings();
            this.pathToConfig = this.OwnFolder() + System.IO.Path.DirectorySeparatorChar;
            ctt = new CardTextTranslator(pathToConfig, sttngs);
            ctt.googlekeys.Add("DE", "0AhhxijYPL-BGdDJaWTI4UVJ3OUZfYzlCSWo3dkZSOXc");
            ctt.googlekeys.Add("FR", "0AsfEX06xqzPEdE9lQlg5NFg2ejBRamltMEhta2FrX2c");
            ctt.googlekeys.Add("RU", "0AsYOnt3MiugydDRRUEp4eXU0VUloYUxiSW5nVXl0Y1E");
            ctt.googlekeys.Add("SP", "0AprX3iUTAgX9dDcyUUhQSnVndkxCSjVXTzJ6NDA0c3c");
            //ctt.googlekeys.Add("ENN", "0AhhxijYPL-BGdG1uNXY5WkhJaW1yNm4yaXpMazlaQ3c"); // for checking the scrolls who are changed!
            

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
			return 10;
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
                     scrollsTypes["CardView"].Methods.GetMethod("createText_PassiveAbilities")[0], // for changeing the font 
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

            if (info.target is Communicator && info.targetMethod.Equals("sendRequest") && info.arguments[0] is RoomChatMessageMessage && (info.arguments[0] as RoomChatMessageMessage).text.StartsWith("/language "))
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
                    rcmm.text = "the language was changed to " + language;

                    if (language == "RU") sttngs.usedFont = -1; // special for crylic

                    if (ctt.notTranslatedScrolls)
                    {
                        rcmm.text = rcmm.text + "\r\n" + "some scrolls-translations are outdated, these were not translated";
                    }
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
            if (info.target is GlobalMessageHandler && info.targetMethod.Equals("handleMessage") && info.arguments[0] is CardTypesMessage)
            {
                CardTypesMessage msg = (CardTypesMessage)info.arguments[0];
                ctt.incommingCardTypesMessage(msg);
                
                
            }

            // change font of card
            if (info.target is CardView && info.targetMethod.Equals("createText_PassiveAbilities"))//createTexts
            {
                //some exceptions for russian language (crylic is stupid ;_;)
                if (sttngs.usedFont == 0 && sttngs.usedLanguage == "RU")
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

                if (sttngs.usedFont == -1 && sttngs.usedLanguage == "RU")
                {
                    // change the font/size/alingment
                    List<GameObject> Images = (List<GameObject>)this.textsArrField.GetValue(info.target);
                    Font ffont = (Font)Resources.Load("Fonts/arial", typeof(Font));
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
                    Font ffont = (Font)Resources.Load("Fonts/arial", typeof(Font));
                    //some fonts I found in scrolls
                    if (sttngs.usedFont == 1) { ffont = (Font)Resources.Load("Fonts/arial", typeof(Font)); }
                    if (sttngs.usedFont == 2) { ffont = (Font)Resources.Load("Fonts/HoneyMeadBB_bold", typeof(Font)); }
                    if (sttngs.usedFont == 3) { ffont = (Font)Resources.Load("Fonts/HoneyMeadBB_boldital", typeof(Font)); }
                    if (sttngs.usedFont == 4) { ffont = (Font)Resources.Load("Fonts/dwarvenaxebb", typeof(Font)); }

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
