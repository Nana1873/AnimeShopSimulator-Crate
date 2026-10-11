using System;
using System.Collections.Generic;

namespace AnimeShopCrate
{
    /// <summary>
    /// Mod texts in the game's languages, picked by I2's current language code (exact code first,
    /// then the part before "-"), English as fallback. Keys starting with "ASC/" are key prompts.
    /// </summary>
    internal static class Text
    {
        private static readonly string[] Keys =
        {
            "ASC/put", "ASC/take", "ASC/trash", "ASC/select", "ASC/drop", "ASC/place", "ASC/cancel", "ASC/rotate", "ASC/pickup",
            "crate", "pieces", "space", "empty",
            "w.nothing", "w.crate_empty", "w.trash_off", "w.not_empty", "w.other_crate", "w.slot_empty", "w.closed_box",
            "w.crate_full", "w.wrong_product", "w.wrong_slot", "w.slot_full", "w.elsewhere", "w.not_allowed",
            "wheel.header", "wheel.details",
        };

        // One row per language, values in the order of Keys. "{0}" in w.not_allowed is the product name.
        private static readonly Dictionary<string, string[]> Rows = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new[]
            {
                "Put on shelf", "Into crate", "Throw away", "Choose product", "Set crate down", "Place", "Cancel", "Rotate", "Pick up crate",
                "Crate", "products", "Space", "empty – right-click a shelf slot",
                "The crate can't take or put anything here.", "The crate is empty.", "Throwing products away is disabled in this lobby.",
                "Your crate isn't empty – empty it or set it down first.", "That's another player's crate.", "There's nothing here.",
                "The crate can't take from closed boxes.", "The crate is full.", "This slot holds a different product.",
                "That doesn't fit in this slot.", "This slot is full.", "Your crate is still standing somewhere – pick it up first.",
                "{0} doesn't fit in the crate.", "Crate", "Collect and shelve products",
            },
            ["de"] = new[]
            {
                "Einräumen", "In die Kiste", "Wegwerfen", "Produkt wählen", "Kiste abstellen", "Platzieren", "Abbrechen", "Drehen", "Kiste aufheben",
                "Kiste", "Produkte", "Platz", "leer – Rechtsklick auf ein Regalfach",
                "Hier kann die Kiste nichts nehmen oder einräumen.", "Die Kiste ist leer.", "Produkte wegwerfen ist in dieser Lobby ausgeschaltet.",
                "Deine Kiste ist nicht leer – erst ausräumen oder abstellen.", "Das ist die Kiste von einem Mitspieler.", "Hier liegt nichts.",
                "Aus geschlossenen Kartons kann die Kiste nichts nehmen.", "Die Kiste ist voll.", "In diesem Fach liegt ein anderes Produkt.",
                "Das passt nicht in dieses Fach.", "Das Fach ist voll.", "Deine Kiste steht noch irgendwo – heb sie zuerst wieder auf.",
                "{0} passt nicht in die Kiste.", "Kiste", "Produkte sammeln und einräumen",
            },
            ["fr"] = new[]
            {
                "Ranger", "Dans la caisse", "Jeter", "Choisir le produit", "Poser la caisse", "Placer", "Annuler", "Tourner", "Ramasser la caisse",
                "Caisse", "produits", "Place", "vide – clic droit sur un emplacement",
                "La caisse ne peut rien prendre ni ranger ici.", "La caisse est vide.", "Jeter des produits est désactivé dans ce salon.",
                "Ta caisse n'est pas vide – vide-la ou pose-la d'abord.", "C'est la caisse d'un autre joueur.", "Il n'y a rien ici.",
                "La caisse ne peut pas prendre dans des cartons fermés.", "La caisse est pleine.", "Cet emplacement contient un autre produit.",
                "Ça ne rentre pas dans cet emplacement.", "Cet emplacement est plein.", "Ta caisse est encore posée quelque part – ramasse-la d'abord.",
                "{0} ne rentre pas dans la caisse.", "Caisse", "Ramasser et ranger des produits",
            },
            ["es"] = new[]
            {
                "Reponer", "A la caja", "Tirar", "Elegir producto", "Dejar la caja", "Colocar", "Cancelar", "Girar", "Recoger la caja",
                "Caja", "productos", "Espacio", "vacía – clic derecho en un hueco",
                "La caja no puede coger ni reponer nada aquí.", "La caja está vacía.", "Tirar productos está desactivado en esta sala.",
                "Tu caja no está vacía: vacíala o déjala primero.", "Es la caja de otro jugador.", "Aquí no hay nada.",
                "La caja no puede coger de cajas cerradas.", "La caja está llena.", "En este hueco hay otro producto.",
                "Eso no cabe en este hueco.", "Este hueco está lleno.", "Tu caja sigue en algún sitio: recógela primero.",
                "{0} no cabe en la caja.", "Caja", "Recoger y reponer productos",
            },
            ["it"] = new[]
            {
                "Sistema", "Nella cassa", "Butta", "Scegli prodotto", "Posa la cassa", "Posiziona", "Annulla", "Ruota", "Raccogli la cassa",
                "Cassa", "prodotti", "Spazio", "vuota – clic destro su uno spazio",
                "La cassa non può prendere né sistemare nulla qui.", "La cassa è vuota.", "Buttare prodotti è disattivato in questa lobby.",
                "La tua cassa non è vuota: svuotala o posala prima.", "È la cassa di un altro giocatore.", "Qui non c'è niente.",
                "La cassa non può prendere da scatole chiuse.", "La cassa è piena.", "In questo spazio c'è un altro prodotto.",
                "Non ci sta in questo spazio.", "Questo spazio è pieno.", "La tua cassa è ancora posata da qualche parte: raccoglila prima.",
                "{0} non entra nella cassa.", "Cassa", "Raccogli e sistema prodotti",
            },
            ["pt"] = new[]
            {
                "Repor", "Na caixa", "Jogar fora", "Escolher produto", "Largar caixa", "Posicionar", "Cancelar", "Girar", "Pegar caixa",
                "Caixa", "produtos", "Espaço", "vazia – clique direito num espaço da prateleira",
                "A caixa não pode pegar nem repor nada aqui.", "A caixa está vazia.", "Jogar produtos fora está desativado neste lobby.",
                "Sua caixa não está vazia – esvazie ou largue primeiro.", "Essa é a caixa de outro jogador.", "Não há nada aqui.",
                "A caixa não pode pegar de caixas fechadas.", "A caixa está cheia.", "Este espaço tem outro produto.",
                "Isso não cabe neste espaço.", "Este espaço está cheio.", "Sua caixa ainda está em algum lugar – pegue-a primeiro.",
                "{0} não cabe na caixa.", "Caixa", "Pegue e reponha produtos",
            },
            ["ru"] = new[]
            {
                "Выложить", "В ящик", "Выбросить", "Выбрать товар", "Поставить ящик", "Поставить", "Отмена", "Повернуть", "Взять ящик",
                "Ящик", "шт.", "Место", "пусто – ПКМ по ячейке полки",
                "Здесь ящик ничего не может взять или выложить.", "Ящик пуст.", "Выбрасывать товары в этом лобби запрещено.",
                "Твой ящик не пуст – сначала разгрузи или поставь его.", "Это ящик другого игрока.", "Здесь ничего нет.",
                "Из закрытых коробок ящик брать не может.", "Ящик полон.", "В этой ячейке другой товар.",
                "Это не подходит для этой ячейки.", "Ячейка заполнена.", "Твой ящик ещё где-то стоит – сначала подними его.",
                "{0} не помещается в ящик.", "Ящик", "Собирать и выкладывать товары",
            },
            ["uk"] = new[]
            {
                "Викласти", "У ящик", "Викинути", "Обрати товар", "Поставити ящик", "Поставити", "Скасувати", "Повернути", "Взяти ящик",
                "Ящик", "шт.", "Місце", "порожньо – ПКМ по комірці полиці",
                "Тут ящик нічого не може взяти чи викласти.", "Ящик порожній.", "Викидати товари в цьому лобі заборонено.",
                "Твій ящик не порожній – спершу розвантаж або постав його.", "Це ящик іншого гравця.", "Тут нічого немає.",
                "Із закритих коробок ящик брати не може.", "Ящик повний.", "У цій комірці інший товар.",
                "Це не підходить для цієї комірки.", "Комірка заповнена.", "Твій ящик ще десь стоїть – спершу підніми його.",
                "{0} не вміщується в ящик.", "Ящик", "Збирати й викладати товари",
            },
            ["pl"] = new[]
            {
                "Wyłóż", "Do skrzynki", "Wyrzuć", "Wybierz produkt", "Odstaw skrzynkę", "Postaw", "Anuluj", "Obróć", "Podnieś skrzynkę",
                "Skrzynka", "szt.", "Miejsce", "pusta – PPM na miejscu na półce",
                "Skrzynka nie może tu nic wziąć ani wyłożyć.", "Skrzynka jest pusta.", "Wyrzucanie produktów jest wyłączone w tym lobby.",
                "Twoja skrzynka nie jest pusta – najpierw ją opróżnij lub odstaw.", "To skrzynka innego gracza.", "Nic tu nie ma.",
                "Skrzynka nie może brać z zamkniętych kartonów.", "Skrzynka jest pełna.", "W tym miejscu jest inny produkt.",
                "To nie pasuje do tego miejsca.", "To miejsce jest pełne.", "Twoja skrzynka nadal gdzieś stoi – najpierw ją podnieś.",
                "{0} nie mieści się w skrzynce.", "Skrzynka", "Zbieraj i wykładaj produkty",
            },
            ["tr"] = new[]
            {
                "Rafa koy", "Kasaya al", "At", "Ürün seç", "Kasayı bırak", "Yerleştir", "İptal", "Döndür", "Kasayı al",
                "Kasa", "ürün", "Alan", "boş – bir raf yerine sağ tıkla",
                "Kasa burada bir şey alamaz veya koyamaz.", "Kasa boş.", "Bu lobide ürün atmak kapalı.",
                "Kasan boş değil – önce boşalt ya da bırak.", "Bu başka bir oyuncunun kasası.", "Burada bir şey yok.",
                "Kasa kapalı kolilerden alamaz.", "Kasa dolu.", "Bu yerde başka bir ürün var.",
                "Bu, bu yere sığmaz.", "Bu yer dolu.", "Kasan hâlâ bir yerde duruyor – önce onu al.",
                "{0} kasaya sığmıyor.", "Kasa", "Ürün topla ve rafa diz",
            },
            ["ja"] = new[]
            {
                "棚に並べる", "かごに入れる", "捨てる", "商品を選ぶ", "かごを置く", "置く", "キャンセル", "回転", "かごを持つ",
                "かご", "個", "容量", "空です – 棚を右クリック",
                "ここではかごで取ることも並べることもできません。", "かごは空です。", "このロビーでは商品を捨てることができません。",
                "かごが空ではありません。先に空にするか置いてください。", "これは他のプレイヤーのかごです。", "ここには何もありません。",
                "閉じた箱からは取れません。", "かごがいっぱいです。", "この棚には別の商品があります。",
                "この棚には置けません。", "この棚はいっぱいです。", "かごがまだどこかに置いてあります。先に拾ってください。",
                "{0}はかごに入りません。", "かご", "商品を集めて棚に並べる",
            },
            ["ko"] = new[]
            {
                "진열하기", "상자에 담기", "버리기", "상품 선택", "상자 내려놓기", "놓기", "취소", "회전", "상자 들기",
                "상자", "개", "공간", "비어 있음 – 진열칸을 우클릭",
                "여기서는 상자로 가져오거나 진열할 수 없습니다.", "상자가 비어 있습니다.", "이 로비에서는 상품을 버릴 수 없습니다.",
                "상자가 비어 있지 않습니다. 먼저 비우거나 내려놓으세요.", "다른 플레이어의 상자입니다.", "여기에는 아무것도 없습니다.",
                "닫힌 박스에서는 꺼낼 수 없습니다.", "상자가 가득 찼습니다.", "이 칸에는 다른 상품이 있습니다.",
                "이 칸에는 맞지 않습니다.", "이 칸이 가득 찼습니다.", "상자가 아직 어딘가에 놓여 있습니다. 먼저 들어 주세요.",
                "{0}은(는) 상자에 들어가지 않습니다.", "상자", "상품을 모아 진열하기",
            },
            ["zh-CN"] = new[]
            {
                "上架", "放入箱子", "丢弃", "选择商品", "放下箱子", "放置", "取消", "旋转", "拿起箱子",
                "箱子", "件", "空间", "空 – 右键点击货架格",
                "箱子无法在这里拿取或上架。", "箱子是空的。", "此大厅已禁止丢弃商品。",
                "你的箱子不是空的——请先清空或放下。", "这是其他玩家的箱子。", "这里什么都没有。",
                "无法从未打开的纸箱中拿取。", "箱子已满。", "这个格子里是其他商品。",
                "这个放不进这个格子。", "这个格子已满。", "你的箱子还放在某处——请先拿起它。",
                "{0}放不进箱子。", "箱子", "收集并上架商品",
            },
            ["zh-TW"] = new[]
            {
                "上架", "放入箱子", "丟棄", "選擇商品", "放下箱子", "放置", "取消", "旋轉", "拿起箱子",
                "箱子", "件", "空間", "空 – 右鍵點擊貨架格",
                "箱子無法在這裡拿取或上架。", "箱子是空的。", "此大廳已禁止丟棄商品。",
                "你的箱子不是空的——請先清空或放下。", "這是其他玩家的箱子。", "這裡什麼都沒有。",
                "無法從未打開的紙箱中拿取。", "箱子已滿。", "這個格子裡是其他商品。",
                "這個放不進這個格子。", "這個格子已滿。", "你的箱子還放在某處——請先拿起它。",
                "{0}放不進箱子。", "箱子", "收集並上架商品",
            },
            ["cs"] = new[]
            {
                "Doplnit", "Do přepravky", "Vyhodit", "Vybrat produkt", "Položit přepravku", "Umístit", "Zrušit", "Otočit", "Zvednout přepravku",
                "Přepravka", "produktů", "Místo", "prázdná – pravým klikem na přihrádku",
                "Tady přepravka nemůže nic vzít ani doplnit.", "Přepravka je prázdná.", "Vyhazování produktů je v této lobby vypnuté.",
                "Tvoje přepravka není prázdná – nejdřív ji vyprázdni nebo polož.", "To je přepravka jiného hráče.", "Nic tu není.",
                "Z uzavřených krabic přepravka nic nevezme.", "Přepravka je plná.", "V této přihrádce je jiný produkt.",
                "To se do této přihrádky nevejde.", "Přihrádka je plná.", "Tvoje přepravka ještě někde stojí – nejdřív ji zvedni.",
                "{0} se do přepravky nevejde.", "Přepravka", "Sbírej a doplňuj produkty",
            },
            ["sv"] = new[]
            {
                "Fyll på", "I backen", "Släng", "Välj produkt", "Ställ ner backen", "Placera", "Avbryt", "Rotera", "Plocka upp backen",
                "Back", "produkter", "Plats", "tom – högerklicka på en hyllplats",
                "Backen kan inte ta eller fylla på något här.", "Backen är tom.", "Att slänga produkter är avstängt i den här lobbyn.",
                "Din back är inte tom – töm den eller ställ ner den först.", "Det är en annan spelares back.", "Här finns inget.",
                "Backen kan inte ta från stängda lådor.", "Backen är full.", "Den här platsen har en annan produkt.",
                "Det får inte plats på den här platsen.", "Den här platsen är full.", "Din back står fortfarande någonstans – plocka upp den först.",
                "{0} får inte plats i backen.", "Back", "Samla och fyll på produkter",
            },
            ["vi"] = new[]
            {
                "Xếp lên kệ", "Bỏ vào thùng", "Vứt bỏ", "Chọn sản phẩm", "Đặt thùng xuống", "Đặt", "Hủy", "Xoay", "Nhặt thùng",
                "Thùng", "sản phẩm", "Chỗ", "trống – nhấp chuột phải vào một ô kệ",
                "Thùng không thể lấy hoặc xếp gì ở đây.", "Thùng đang trống.", "Vứt bỏ sản phẩm đã bị tắt trong phòng này.",
                "Thùng của bạn chưa trống – hãy dọn trống hoặc đặt xuống trước.", "Đây là thùng của người chơi khác.", "Ở đây không có gì.",
                "Thùng không thể lấy từ hộp chưa mở.", "Thùng đã đầy.", "Ô này chứa sản phẩm khác.",
                "Món này không vừa ô này.", "Ô này đã đầy.", "Thùng của bạn vẫn đang đặt ở đâu đó – hãy nhặt nó lên trước.",
                "{0} không vừa thùng.", "Thùng", "Thu gom và xếp sản phẩm lên kệ",
            },
        };

        private static bool _logged;

        /// <summary>The language row in use (changes when the player switches the game language).</summary>
        internal static string Language
        {
            get
            {
                string code;
                try { code = Il2CppI2.Loc.LocalizationManager.CurrentLanguageCode ?? "en"; }
                catch { code = "en"; }
                if (!_logged) LogLanguages(code);
                if (Rows.ContainsKey(code)) return code;
                int dash = code.IndexOf('-');
                if (dash > 0 && Rows.ContainsKey(code.Substring(0, dash))) return code.Substring(0, dash);
                if (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    return code.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0 || code.IndexOf("HK", StringComparison.OrdinalIgnoreCase) >= 0 ? "zh-TW" : "zh-CN";
                return "en";
            }
        }

        internal static string Get(string key)
        {
            int i = Array.IndexOf(Keys, key);
            if (i < 0) return key;
            var row = Rows[Language];
            return i < row.Length ? row[i] : Rows["en"][i];
        }

        internal static string Format(string key, object arg) => string.Format(Get(key), arg);

        private static void LogLanguages(string current)
        {
            _logged = true;
            try
            {
                var codes = Il2CppI2.Loc.LocalizationManager.GetAllLanguagesCode(true, true);
                var missing = new List<string>();
                var all = new List<string>();
                for (int i = 0; codes != null && i < codes.Count; i++)
                {
                    string c = codes[i];
                    all.Add(c);
                    int dash = c.IndexOf('-');
                    bool known = Rows.ContainsKey(c) || (dash > 0 && Rows.ContainsKey(c.Substring(0, dash))) || c.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
                    if (!known) missing.Add(c);
                }
                Settings.Debug($"game languages: {string.Join(", ", all)} (current {current})" +
                               (missing.Count > 0 ? $"; no crate translation yet for {string.Join(", ", missing)} (English is used)" : ""));
            }
            catch (Exception e) { Settings.Debug("reading the game's languages failed: " + e.Message); }
        }
    }
}
