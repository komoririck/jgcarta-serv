using hololive_oficial_cardgame_server.SerializableObjects;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace hololive_oficial_cardgame_server.WebSocketDuelFunctions
{
    internal class SuporteEffectAttachEnergyIfResponseHandler
    {
        private PlayerRequest _ReturnData;
        private DuelAction _DuelAction;

        //we may want to add the location here later to change from here the player is getting the cheer, by now, cheerdeck
        internal async Task SuporteEffectAttachEnergyHandleAsync(PlayerRequest playerRequest, MatchRoom cMatchRoom, string selectEnergyToAttach = null)
        {
            ///////Reference of the variables////////////////////////////
            // usually we add the duelAction values to the temphand
            // tempHand.Add(_DuelAction.usedCard);
            // tempHand.Add(_DuelAction.cheerCostCard);
            // tempHand.Add(_DuelAction.targetCard);
            ///////Reference of the variables////////////////////////////

            //we recieve from the client the energy that we pick in the list displayed if was not send when called the function
            if (selectEnergyToAttach == null) {
                DuelAction _DuelActionRecieved = JsonSerializer.Deserialize<DuelAction>(playerRequest.requestObject);
                List<object> ResponseObjList = JsonSerializer.Deserialize<List<object>>(_DuelActionRecieved.actionObject);
                List<string> list = (List<string>)ResponseObjList[0];
                selectEnergyToAttach = list[0];
            }

            //tempHandd is the resolving card
            List<Card> tempHandd = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerATempHand : cMatchRoom.playerBTempHand;

            //energy list
            List<Card> playerCheerDeck = cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID ? cMatchRoom.playerACardCheer : cMatchRoom.playerBCardCheer;

            //if the picked energy exist in the user cheer card list
            int icounter = 0;
            int jcounter = 0;
            foreach (Card existInCheer in playerCheerDeck)
            {
                if (existInCheer.cardNumber.Equals(selectEnergyToAttach))
                {
                    jcounter = icounter;
                    break;
                }
                icounter++;
            }

            //getting the information for the energy at the cheer list position
            playerCheerDeck[jcounter].GetCardInfo();

            //special scenario for the card "hBP01-105" which needs to match colors
            if (tempHandd[0].cardNumber.Equals("hBP01-105")) //tempHandd[0] used card
                if (!tempHandd[2].color.Equals(playerCheerDeck[jcounter].color)) // tempHandd[2] target card
                    return;

            //removing the card from the list since we will attach to a card
            playerCheerDeck.RemoveAt(jcounter);

            //some calls of the function didnt add cards to the holder, so lets instantiate a new card where shold be the energy
            if (tempHandd[1] == null)
            {
                //Add the energy from the json here, since has empty, we can do this because the checks are already done
                //sometimes temphand[1] is null, so we need to assign the value before sending back to player
                tempHandd[1] = new Card(selectEnergyToAttach);
            }

            //creating the action to send to the player
            _DuelAction = new()
            {
                local = tempHandd[2].cardPosition,
                playerID = cMatchRoom.currentPlayerTurn,
                playedFrom = "CardCheer",
                usedCard = tempHandd[1],
                targetCard = tempHandd[2]
            };


            if (cMatchRoom.currentPlayerTurn == cMatchRoom.playerA.PlayerID)
                cMatchRoom.AssignEnergyToZone(_DuelAction, cMatchRoom.playerAStage, cMatchRoom.playerACollaboration, cMatchRoom.playerABackPosition);
            else
                cMatchRoom.AssignEnergyToZone(_DuelAction, cMatchRoom.playerBStage, cMatchRoom.playerBCollaboration, cMatchRoom.playerBBackPosition);

            _ReturnData = new PlayerRequest { type = "DuelUpdate", description = "AttachEnergyResponse", requestObject = JsonSerializer.Serialize(_DuelAction, Lib.jsonOptions) };


            cMatchRoom.RecordPlayerRequest(cMatchRoom.ReplicatePlayerRequestForOtherPlayers(cMatchRoom.GetPlayers(), _ReturnData));
            cMatchRoom.PushPlayerAnswer();

            //cleaning temphand
            tempHandd.Clear();

            //removing resolving card
            cMatchRoom.currentCardResolving = "";
            cMatchRoom.currentGameHigh++;
        }
    }
}