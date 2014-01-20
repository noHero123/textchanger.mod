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

        private string pathToConfig = "";
        int usedFont = 0;


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
            
            this.pathToConfig = this.OwnFolder() + System.IO.Path.DirectorySeparatorChar;
            ctt = new CardTextTranslator(pathToConfig);
            ctt.googlekeys.Add("DE", "0AhhxijYPL-BGdDJaWTI4UVJ3OUZfYzlCSWo3dkZSOXc");
            ctt.googlekeys.Add("FR", "0AsfEX06xqzPEdE9lQlg5NFg2ejBRamltMEhta2FrX2c");
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
			return 6;
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
                     scrollsTypes["CardView"].Methods.GetMethod("createTexts")[0], // for changeing the font 
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
                        usedFont = 1;
                        rcmm.text ="Font was changed to arial";
                    }
                    else
                    {
                        if (choosenFont == "honey1")
                        {
                            usedFont = 2;
                            rcmm.text = "Font was changed to honey1";
                        }
                        else
                        {
                            if (choosenFont == "honey2")
                            {
                                usedFont = 3;
                                rcmm.text = "Font was changed to honey2";
                            }
                            else
                            {
                                if (choosenFont == "dwar")
                                {
                                    usedFont = 4;
                                    rcmm.text = "Font was changed to dwar";
                                }
                                else
                                {
                                    usedFont = 0;
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
            if (info.target is CardView && info.targetMethod.Equals("createTexts") && usedFont >= 1)
            {
                Console.WriteLine("change font");
                Font ffont = (Font)Resources.Load("Fonts/arial", typeof(Font));
                //some fonts I found in scrolls
                if (usedFont == 1) { ffont = (Font)Resources.Load("Fonts/arial", typeof(Font)); }
                if (usedFont == 2) { ffont = (Font)Resources.Load("Fonts/HoneyMeadBB_bold", typeof(Font)); }
                if (usedFont == 3) { ffont = (Font)Resources.Load("Fonts/HoneyMeadBB_boldital", typeof(Font)); }
                if (usedFont == 4) { ffont = (Font)Resources.Load("Fonts/dwarvenaxebb", typeof(Font)); }

                // change the font/size/alingment
                FieldInfo textsArrField;
                textsArrField = typeof(CardView).GetField("textsArr", BindingFlags.Instance | BindingFlags.NonPublic);
                List<GameObject> Images = (List<GameObject>)textsArrField.GetValue(info.target);
                ffont.material.color = Color.blue;
                foreach (GameObject go in Images)
                {
                    TextMesh lol = go.GetComponentInChildren<TextMesh>();
                    ffont.material.color = go.renderer.material.color;
                    lol.font = ffont;
                    go.renderer.material = ffont.material;

                    //lol.anchor = TextAnchor.MiddleCenter;
                    if(usedFont == 0) lol.fontSize = (int)(lol.fontSize * 0.8);
                }

            }
         

            //returnValue = null;
            return;//return false;
        }



        
	}
}
