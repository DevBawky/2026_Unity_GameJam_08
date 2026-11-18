using Unity.Netcode;
using UnityEngine;

// 구간 완료 후 돌봄 점수·AP 정산, 마왕 보고, 업그레이드 투표와 구매.
public sealed partial class CartStations
{
    public NetworkVariable<Unity.Collections.FixedString512Bytes> KingReply = new();
    public const int Departure = 8;
    public bool CanBuy(int choice)
    {
        var card = CartRules.Active.GetCard(choice);
        return Affection.Value >= card.price && (choice == 0 ? durability < Capacity || wheelDetached : CardLevel(choice) < card.maxLevel);
    }
    public static readonly string[] Reports = { "왕족 돌봄", "마차 피해", "포탄 보급" };

    private void FinishCare()
    {
        PrincePoints.Value=CarePoints(0);
        PrincessPoints.Value=CarePoints(1);
        Affection.Value+=PrincePoints.Value+PrincessPoints.Value;
        ShopRound.Value++;
    }
    private int CarePoints(int i)
    {
        int n=Mathf.Max(1,careTicks);
        int seatPercent = Mathf.FloorToInt(100f * seated[i] / n + .5f);
        int attention = Mathf.FloorToInt((float)(attentionSum[i] / n) + .5f);
        int score = Mathf.FloorToInt((seatPercent * .6f + attention * .4f) / 10f + .5f);
        int penalty=crying[i]==0 ? 0 : Mathf.Clamp(Mathf.FloorToInt(4f*crying[i]/n + .5f),1,4);
        return Mathf.Max(0,score-penalty);
    }
    public void ReceiveChoice(NetworkPlayer actor,int choice,int round,int stage)
    {
        if(!IsServer || !ChoosingUpgrade || !actor.Assigned.Value || round!=ShopRound.Value || stage!=stageNumber) return;
        if(Report.Value<0)
        {
            if(actor.OwnerClientId!=NetworkManager.ServerClientId || choice<0 || choice>=Reports.Length) return;
            // 추후 변경점: 보고 답변은 방장 언어로 만들어 네트워크로 보낸다. 참가자마다 다른 언어를 쓰려면 템플릿 번호와 숫자만 보내야 한다.
            string reply = choice switch {
                0 => LocalizationTable.T(crying[0]+crying[1]>0 ? "울었던 시간만큼 감점했다. 놀라면 안고 걷고, 배고프면 간식을 주어라." :
                    PrincePoints.Value+PrincessPoints.Value>=14 ? "두 아이를 잘 돌봤군." : "자리를 비운 아이는 자기 좌석으로 데려다주어라."),
                1 => LocalizationTable.F("내구도 {0}/{1:0}. ", Mathf.RoundToInt(durability), Capacity) + LocalizationTable.T(wheelDetached ? "빠진 바퀴를 수리할지 상의해라." : "수리와 보강 중 무엇이 필요한지 상의해라."),
                _ => LocalizationTable.F("남은 포탄 {0}발, 분실 {1}발. 장전수와 운반 담당이 연락하게 해라.", shellsInPile+depositedShells+(loaded?1:0), lostShells)
            };
            KingReply.Value = new Unity.Collections.FixedString512Bytes(reply);
            Report.Value=choice; ShopRound.Value++; return;
        }
        if(choice<0 || choice>Departure) return;
        if(choice<Departure && !CanBuy(choice)) return;
        Votes[actor.Slot.Value]=choice;
        TallyChoices();
    }

    public void TallyChoices()
    {
        if(!IsServer || !ChoosingUpgrade || Report.Value<0) return;
        int humans=0;
        foreach(var p in gameScene.Players) if(p.Assigned.Value && !p.Bot.Value) humans++;
        if(humans==0) return;
        for(int choice=0;choice<=Departure;choice++)
        {
            int votes=0;
            foreach(var p in gameScene.Players) if(p.Assigned.Value && !p.Bot.Value && Votes[p.Slot.Value]==choice) votes++;
            if(votes<=humans/2) continue;
            ShopRound.Value++;
            for(int i=0;i<Votes.Count;i++) Votes[i]=-1;
            if(choice==Departure) { NextStage(); return; }
            var card = CartRules.Active.GetCard(choice);
            Affection.Value -= card.price;
            switch(choice)
            {
                case 0: durability=Mathf.Min(Capacity,durability+card.amount); if(wheelDetached) RepairWheel(); break;
                case 1: precisionLevel++; break;
                case 2: Stabilizer.Value++; break;
                case 3: Barrel.Value++; break;
                case 4: Hull.Value++; durability+=card.amount; break;
                case 5: Porter.Value++; break;
                case 6: cushionAxleLevel++; break;
                case 7: horseLevel++; break;
            }
            Publish();
            return;
        }
    }
    private int CardLevel(int i) => i switch {1=>precisionLevel,2=>Stabilizer.Value,3=>Barrel.Value,4=>Hull.Value,5=>Porter.Value,6=>cushionAxleLevel,7=>horseLevel,_=>0};
}
