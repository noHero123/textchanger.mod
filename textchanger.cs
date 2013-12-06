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
			return 1;
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
                Console.WriteLine("##start");
                RoomChatMessageMessage rcmm= info.arguments[0] as RoomChatMessageMessage;
                rcmm.from = "LanguageChanger";
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

           
         

            //returnValue = null;
            return;//return false;
        }



        
	}
}
