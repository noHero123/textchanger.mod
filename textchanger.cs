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


namespace textchanger.mod
{
    public class textchanger : BaseMod
	{


        private FieldInfo descriptionField;
        private FieldInfo flavorField;


		//initialize everything here, Game is loaded at this point
        public textchanger()
		{
            descriptionField = typeof(CardType).GetField("description", BindingFlags.Instance | BindingFlags.NonPublic);
            flavorField = typeof(CardType).GetField("flavor", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                
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

             };
            }
            catch
            {
                return new MethodDefinition[] { };
            }
		}


        public override bool WantsToReplace(InvocationInfo info)
        {

            return false;
        }

        public override void ReplaceMethod(InvocationInfo info, out object returnValue)
        {
            returnValue = null;
        }

        public override void BeforeInvoke(InvocationInfo info)
        {

            return;

        }

        private int getindexfromcardtypearray(CardType[] cts,string cardname)
        {
            int retval =-1;

            for (int i = 0; i < cts.Length; i++)
            {
                if (cts[i].name.ToLower() == cardname.ToLower())
                {
                    retval = i;
                    break;
                }
            }
                return retval;
        }


        public override void AfterInvoke (InvocationInfo info, ref object returnValue)
        //public override bool BeforeInvoke(InvocationInfo info, out object returnValue)
        {
            if (info.target is GlobalMessageHandler && info.targetMethod.Equals("handleMessage"))
            {
                string path = Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "cardtypesmsg.txt";
                CardTypesMessage msg = (CardTypesMessage)info.arguments[0];
                CardType[] cts = msg.cardTypes;
                string lol = File.ReadAllText(path);
                JsonReader jsonReader = new JsonReader();
                Dictionary<string, object> dictionary = (Dictionary<string, object>)jsonReader.Read(lol);
                Dictionary<string, object>[] d = (Dictionary<string, object>[])dictionary["cardTypes"];

                for (int i = 0; i < d.GetLength(0); i++)
                {
                    int cardid = Convert.ToInt32(d[i]["id"]);//no need, but why not?:D
                    string cardname = d[i]["name"].ToString();
                    string description = d[i]["description"].ToString();
                    string flavor = d[i]["flavor"].ToString();
                    //get index from cts
                    int ctsindex=getindexfromcardtypearray(cts,cardname);
                    //change description
                    this.descriptionField.SetValue(cts[ctsindex],description);
                    //change flavor
                    this.flavorField.SetValue(cts[ctsindex], flavor);
                }
                //reset cardtypemanager
                CardTypeManager.getInstance().reset();
                //feed with edited cardtypes
                CardTypeManager.getInstance().feed(cts);
                //write lol in output_log, because if the mod doesnt do anything, so hes doing something!
                Console.WriteLine("lol#");
            }

           
         

            //returnValue = null;
            return;//return false;
        }



        
	}
}

