using System;
using Conquer_Online_Server.Network.GamePackets;

namespace Conquer_Online_Server
{
    public class Constants
    {
        public static readonly Message FullInventory = new Message("Não há espaço suficiente no seu inventário!", System.Drawing.Color.Red, Message.TopLeft),
            OneFlowerADay = new Message("Você só pode enviar uma flor por dia", System.Drawing.Color.Red, Message.TopLeft),
            TradeRequest = new Message("Pedido de troca enviado.", System.Drawing.Color.Red, Message.TopLeft),
            TradeInventoryFull = new Message("Não há espaço suficiente no inventário do seu parceiro.", System.Drawing.Color.Red, Message.TopLeft),
            TradeInProgress = new Message("Um comércio já está em andamento. Tente novamente mais tarde.", System.Drawing.Color.Red, Message.TopLeft),
            FloorItemNotAvailable = new Message("Aguarde um momento para poder pegar este item!", System.Drawing.Color.Red, Message.TopLeft),
            JailItemUnusable = new Message("Você não pode usar este item aqui!", System.Drawing.Color.Red, Message.TopLeft),
            PKForbidden = new Message("PK proibido neste mapa.", System.Drawing.Color.Red, Message.TopLeft),
            ExpBallsUsed = new Message("Você pode usar apenas dez bolas de exp por dia. Tente amanhã.", System.Drawing.Color.Red, Message.TopLeft),
            SpellLeveled = new Message("Parabéns, você acaba de passar de level o seu feitiço.", System.Drawing.Color.Red, Message.TopLeft),
            ProficiencyLeveled = new Message("Parabéns, você acaba de passar de level a sua proficiência.", System.Drawing.Color.Red, Message.TopLeft),
            ArrowsReloaded = new Message("Flechas recarregadas.", System.Drawing.Color.Red, Message.TopLeft),
            Warrent = new Message("Os guardas estão olhando para você!", System.Drawing.Color.Red, Message.TopLeft),
            VIPExpired = new Message("Seu VIP expirou. Renove seu VIP para manter os benefícios.", System.Drawing.Color.Red, Message.World),
            VIPLifetime = new Message("O seu serviço VIP é ilimitado.", System.Drawing.Color.Red, Message.World),
            WrongAccessory = new Message("Você não pode usar este acessório e este item, ao mesmo tempo.", System.Drawing.Color.Red, Message.World),
            NoAccessory = new Message("Você não pode usar um acessório sem um item de apoio.", System.Drawing.Color.Red, Message.World),
            vipteleport = new Message("Você não pode se teletransportar neste mapa.", System.Drawing.Color.Red, Message.World);

        public static Message VIPRemaining(string days, string hours)
        {
            return new Message("Você tem " + days + " dia(s) e " + hours + " hora(s) de serviço VIP restantes.", System.Drawing.Color.Red, Message.World);
        }
        public static Message NoArrows(string name)
        {
            return new Message("Não é possível recarregar as flechas: você está sem " + name + "!", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message Stigma(float percent, int time)
        {
            return new Message("Estigma ativado. Seu ataque será aumentado com " + percent + " por " + time + ".", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message Accuracy(int time)
        {
            return new Message("Precisão ativada. Sua agilidade aumentou por " + time + " segundos.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message Invisibility(int time)
        {
            return new Message("Invisibilidade ativada. Você ficará invisível para monstros por " + time + " segundos enquanto não atacar.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message Shield(float percent, int time)
        {
            return new Message("Escudo ativado. Sua defesa será aumentada com " + percent + " por " + time + ".", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message Shackled(int time)
        {
            return new Message("Você foi algemado e não pode se mover por " + time + " Segundos.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message Dodge(float percent, int time)
        {
            return new Message("Esquiva ativada. Sua esquiva aumentou em " + percent + " por " + time + " segundos.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message NoDrugs(int time)
        {
            return new Message("Estrela de Veneno ativada. Você não poderá usar poções de vida por " + time + " segundos.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message ExtraExperience(uint experience)
        {
            return new Message("Você ganhou adicional " + experience + " experiência por matar o monstro.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message TeamExperience(uint experience)
        {
            return new Message("Um de seus companheiros de equipe matou um monstro e você ganhou " + experience + " experiencia.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message NoobTeamExperience(uint experience)
        {
            return new Message("Um membro da equipe matou um monstro e, pelo bônus de novato na equipe, você ganhou " + experience + " de experiência.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message PickupGold(uint amount)
        {
            return new Message("Você pegou " + amount + " de ouro.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message AzureShield(float percent, int time)
        {
            return new Message("Escudo Lazúli ativado. Sua defesa aumentou em " + percent + " por " + time + " segundos.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message PickupConquerPoints(uint amount)
        {
            return new Message("Você pegou " + amount + " CPs.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message PickupItem(string name)
        {
            return new Message("Você pegou " + name + ".", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message DummyLevelTooHigh()
        {
            return new Message("Seu nível não é alto o suficiente para atacar este boneco de treino.", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message BoothItemSell(string buyername, string itemname, bool conquerpoints, uint cost)
        {
            return new Message("Parabéns! Você vendeu " + itemname + " para " + buyername + " por " + cost + (conquerpoints ? " CPs." : " de ouro."), System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message Enchant(int origEnch, int newEnch)
        {
            if (newEnch <= origEnch)
                return new Message("Você não teve sorte. Você não ganhou mais encanto no seu item. Seu encantamento gerado é " + newEnch + ".", System.Drawing.Color.Red, Message.TopLeft);
            else
                return new Message("Você teve sorte. Você ganhou mais encanto no seu item. Seu encantamento gerado é " + newEnch + ".", System.Drawing.Color.Red, Message.TopLeft);
        }
        public static Message VoteSpan(Client.GameClient client)
        {
            if (DateTime.Now <= client.LastVote.AddHours(12))
            {
                TimeSpan agospan = client.LastVote.Subtract(DateTime.Now);
                TimeSpan tillspan = DateTime.Now.Subtract(client.LastVote);
                string message = "Seu Ultimo Voto ";
                if (agospan.Hours >= 0)
                    message += agospan.Hours.ToString() + " Horas, ";
                if (agospan.Minutes >= 0)
                    message += agospan.Minutes.ToString() + " minutos, e ";
                message += agospan.Seconds.ToString() + " atrás. espere por favor";
                if (tillspan.Hours >= 0)
                    message += tillspan.Hours.ToString() + " Horas, ";
                if (tillspan.Minutes >= 0)
                    message += agospan.Minutes.ToString() + " minutos, e ";
                message += tillspan.Seconds.ToString() + " atrás. Para votar novamente!";
                return new Message(message, System.Drawing.Color.Red, Message.TopLeft);
            }
            return new Message("Você não votou nas últimas 12 horas. Vote agora para ganhar um ponto extra!", System.Drawing.Color.Red, Message.TopLeft);
        }
        public const string DataHolderPath = "database\\",
        NpcFilePath = "database\\Npcs.txt",
        DMapsPath = "database\\",
        ShopsPath = "database\\shops\\Shop.dat",
        EShopsPath = "database\\shops\\emoneyshop.ini",
        EShopsV2Path = "database\\shops\\emoneyshopV2.ini",
        HonorShopPath = "database\\shops\\HonorShop.ini",
        RaceShopPath = "database\\shops\\RacePointShop.ini",
        ChampionShopPath = "database\\shops\\GoldenLeagueShop.ini",
        PortalsPath = "database\\Portals.ini",
        RevivePoints = "database\\RevivePoints.ini",
        MonstersPath = "database\\Monsters.txt",
        ItemBaseInfosPath = "database\\Items.txt",
        ItemPlusInfosPath = "database\\ItemsPlus.ini",
        SoulGearInformation = "database\\soulgear.txt",
        UnhandledExceptionsPath = "database\\exceptions\\",
        ServerKey = "TQServer",
        WelcomeMessages = "database\\WelcomeMessages.txt",
        QuizShow = "database\\QuizShow.txt",
        GameCryptographyKey = "C238xs65pjy7HU9Q";
        public static string ServerName = "Oblivion-Co";
        public const int MaxBroadcasts = 50;
        public static uint ExtraExperienceRate, ExtraSpellRate, ExtraProficiencyRate, MoneyDropRate, MoneyDropMultiple, ConquerPointsDropRate, ConquerPointsDropMultiple, ItemDropRate;
        public static string[] ItemDropQualityRates;
        public static string WebAccExt, ServerWebsite, WebVoteExt, WebDonateExt, ServerGMPass;
        public const sbyte pScreenDistance = 19;
        public const sbyte nScreenDistance = 19;
        public const sbyte remScreenDistance = 19;
        public const ushort DisconnectTimeOutSeconds = 10,
            FloorItemSeconds = 20,
            FloorItemAvailableAfter = 15;

        public const ushort SocketOneProgress = 100,
            SocketTwoProgress = 300;

        public static readonly System.Collections.Generic.List<ushort> PKForbiddenMaps = new System.Collections.Generic.List<ushort>()
        {
            1036,
            1002,
            700,
            1039,
            1004,
            1006,
            8880,
            8881,
            1950,
            8800,
            8801,
            8802,
            8803,
            601
        };
        public static readonly System.Collections.Generic.List<ushort> PKFreeMaps = new System.Collections.Generic.List<ushort>()
        {
            2065,
            1038,
            1005, 
            6000,
            6004,
            6001,
            6002, 
            6003,
            1844,
            7001,
            2071,
            1801,
            1508,
            1518,
            7777,
            8877,
            3333,
            1002,
            1090,
            700,
            1509
        };
        public static readonly System.Collections.Generic.List<int> SoulList = new System.Collections.Generic.List<int>()
        {
            
            80032000
        };
        public static readonly System.Collections.Generic.List<int> MaxItems = new System.Collections.Generic.List<int>()
        {
            410439,
            420439, 
            480439,
            610439,
            601439, 
            421439,
            823052,
            824001,
            823043,
            822052,
            800014,
            800017,
            800513,
            822053,
            820056,
            800110,
            800320
        };
        public static readonly System.Collections.Generic.List<int> AvaibleSpells = new System.Collections.Generic.List<int>()
        {
            1380,
            1385, 
            1390,
            1395,
            1400, 
            1405,
            1410,
            1046,
            1045,
            5030,
            7001,
            7002,
            7003,
            7020,
            1415,
1418,
1416,
1417,
1419,
            1360
        
        };
        public static readonly System.Collections.Generic.List<ushort> Damage1Map = new System.Collections.Generic.List<ushort>()
        {
            1844,
            1801
        };
    }
}
