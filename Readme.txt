place your cardtypesmsg.txt file in .../Mojang/Scrolls/game/  and have fun
example is on githup-source (old data, containing only cards till noaidi release)
you can even lower each of the data from (example of gravelock elder):

{"id":1,"name":"Gravelock Elder","description":"Other Gravelock creatures you control gain +1 Attack and +1 Health while Gravelock Elder is in play.","flavor":"Gravelocks look up to their elders... literally.","subTypesStr":"Gravelock","kind":"CREATURE","rarity":2,"hp":5,"ap":3,"ac":2,"costDecay":0,"costOrder":0,"costGrowth":0,"costEnergy":5,"rulesList":["AttackForwardRanged","Move","GravelockStrengthModifier"],"cardImage":479,"animationPreviewImage":445,"animationPreviewInfo":"54.75,50.5,0.25","animationBundle":98,"abilities":[{"id":"Move","name":"Move","description":"Move unit to adjacent tile","cost":{"DECAY":0,"ORDER":0,"ENERGY":0,"GROWTH":0}}],"targetArea":"FORWARD","passiveRules":[{"displayName":"Ranged attack","description":"This unit does not take damage from attacking Spiky units."}],"available":true,"sound":"impact_gravelock_physical"}

to

{"id":1,"name":"Gravelock Elder","description":"Other Gravelock creatures you control gain +1 Attack and +1 Health while Gravelock Elder is in play.","flavor":"Gravelocks look up to their elders... literally."}


note: 	the begin of the textfile : 	{"cardTypes":[... 
	and the end:    		...],"msg":"CardTypes"}
	are necessary!

if you wanna only change gravelock elder then the cardtypemsg.txt could only contain the following line. (other data is keept from the original server-message):

{"cardTypes":[{"id":1,"name":"Gravelock Elder","description":"it does whatever a gravelock elder does!","flavor":"Look at the description, baby!"}],"msg":"CardTypes"}

have fun
noHero