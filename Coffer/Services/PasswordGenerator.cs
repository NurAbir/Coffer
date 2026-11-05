using System.Security.Cryptography;
using System.Text;

namespace Coffer.Services;

public class PasswordGenerator
{
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?";
    private const string Ambiguous = "0O1lI";

    // EFF Long Wordlist (simplified subset for recovery phrases)
    private static readonly string[] RecoveryWords = new[]
    {
        "abacus","abdomen","ability","absent","absorb","abstract","abuse","academy",
        "accent","access","accident","account","accuracy","achieve","acid","acoustic",
        "acquire","acrobat","action","active","actor","actual","adapter","addition",
        "address","advance","advice","aerial","affair","affect","afford","afraid",
        "agency","agenda","agree","aircraft","airport","alarm","album","alcohol",
        "alert","algebra","alien","alive","alley","allow","almost","alpha",
        "already","also","altar","alter","always","amazing","amber","ambient",
        "amount","anchor","ancient","angel","anger","angle","animal","ankle",
        "annual","answer","antenna","anxiety","apart","apology","appeal","appear",
        "apple","apricot","arcade","archer","arena","argue","arise","armor",
        "aroma","around","arrow","artist","aspect","aspire","asset","assign",
        "assist","assume","asthma","athlete","atom","attach","attack","attend",
        "auburn","auction","audio","august","aunt","author","autumn","avenue",
        "average","avoid","awake","award","aware","away","awesome","axis",
        "azure","bacon","badge","bagel","baker","balance","balcony","ballad",
        "ballet","balloon","bamboo","banana","band","bank","banner","banquet",
        "barber","barely","bargain","barrel","base","basic","basin","basket",
        "battle","beach","beacon","beard","beast","beauty","beaver","become",
        "before","behave","behind","being","belief","belong","below","bench",
        "berry","beside","best","better","between","beyond","bicycle","bidder",
        "biggest","bike","billion","binary","binder","biology","bird","birth",
        "bishop","bitter","black","blade","blame","blank","blast","blaze",
        "bleak","blend","bless","blind","blink","bliss","block","blonde",
        "blood","bloom","blouse","blue","blur","board","boast","bodies",
        "boiler","bold","bolt","bomb","bond","bone","bonus","book",
        "boost","boot","border","boring","borrow","boss","bother","bottle",
        "bottom","bounce","boundary","bowl","brain","brake","branch","brand",
        "brave","bread","break","breath","breeze","brick","bride","bridge",
        "brief","bright","bring","brisk","broad","broke","bronze","brother",
        "brown","brush","bubble","bucket","budget","buffalo","buffer","build",
        "bulb","bulk","bull","bundle","bureau","burger","burst","bush",
        "business","busy","butter","button","buyer","buzz","cabin","cable",
        "cactus","cafe","cage","cake","calendar","calf","call","calm",
        "camel","camera","camp","canal","cancel","cancer","candle","candy",
        "canoe","canvas","canyon","capable","capital","captain","capture","carbon",
        "card","career","cargo","carpet","carrot","carry","cart","carve",
        "case","cash","cast","castle","casual","catch","cattle","cause",
        "caution","cave","ceiling","celebrity","cell","cement","center","century",
        "ceramic","cereal","certain","chain","chair","chalk","challenge","chamber",
        "champion","chance","change","channel","chaos","chapter","charge","charm",
        "chart","chase","cheap","check","cheek","cheer","chef","chest",
        "chicken","chief","child","chill","chimney","choice","choose","chorus",
        "chrome","chunk","church","cinema","circle","citizen","city","civil",
        "claim","clarify","clash","class","claw","clay","clean","clear",
        "clerk","click","client","cliff","climate","climb","clinic","clock",
        "clone","close","cloth","cloud","clown","club","clue","cluster",
        "coach","coast","cobra","coconut","code","coffee","cognitive","coil",
        "coin","collect","college","colony","color","column","combat","combine",
        "comedy","comfort","comic","command","comment","common","company","compare",
        "compass","compete","compile","complex","compose","concept","concern","concert",
        "conduct","confirm","connect","consent","consider","consist","console","constant",
        "consume","contact","contain","content","contest","context","control","convert",
        "convey","cook","cool","copper","copy","coral","corner","correct",
        "cost","cotton","couch","could","council","count","country","county",
        "couple","courage","course","court","cousin","cover","crack","craft",
        "crash","crawl","crazy","cream","create","credit","creek","crew",
        "cricket","crime","crisp","critic","crop","cross","crowd","crown",
        "crucial","cruel","cruise","crush","crystal","cube","culture","cupboard",
        "cure","curious","current","curtain","curve","custom","cycle","cynic",
        "dagger","daily","dairy","daisy","damage","dance","danger","dark",
        "dash","data","date","daughter","dawn","daylight","deadline","deal",
        "death","debate","debris","debt","decade","decay","decent","decide",
        "decline","decorate","decrease","dedicate","deer","defeat","defense","define",
        "degree","delay","deliver","delta","demand","demise","democrat","denial",
        "dense","dentist","deny","depart","depend","depict","deposit","depth",
        "deputy","derive","descent","describe","desert","design","desire","desk",
        "destroy","detail","detect","develop","device","devote","diagram","diamond",
        "diary","dictate","diesel","diet","differ","digital","dignity","dilemma",
        "dinner","dinosaur","direct","dirt","disagree","discover","discuss","disease",
        "dish","dismiss","display","distance","distant","divide","divine","dizzy",
        "doctor","document","dodge","dolphin","domain","donate","donor","door",
        "double","doubt","dough","dove","draft","dragon","drain","drama",
        "draw","dream","dress","drift","drill","drink","drive","drop",
        "drought","drown","drum","dryer","duck","duel","duke","dune",
        "during","dusk","dust","dwarf","dwell","dying","dynamic","eager",
        "eagle","early","earth","ease","east","easy","echo","eclipse",
        "ecology","economy","edge","edit","educate","effect","effort","egg",
        "eight","either","elbow","elder","elect","elegant","element","elephant",
        "elite","else","email","embark","embrace","emerge","emotion","empire",
        "employ","empty","enact","enamel","enchant","endless","endorse","enemy",
        "energy","enforce","engage","engine","enjoy","enlist","enough","enrich",
        "ensure","enter","entire","entry","envelope","envy","epic","equal",
        "equip","era","erase","erode","error","escape","essay","essence",
        "estate","eternal","ethics","evacuate","evaluate","even","event","ever",
        "every","evidence","evil","evoke","exact","exam","example","exceed",
        "excel","except","excess","exchange","excite","exclude","excuse","execute",
        "exercise","exhaust","exhibit","exile","exist","exit","exotic","expand",
        "expect","expert","explain","explode","explore","export","expose","express",
        "extend","extra","eye","fabric","face","factor","factory","faculty",
        "fade","fail","faint","fair","faith","fake","fall","false",
        "fame","family","famous","fancy","farmer","fashion","fast","fatal",
        "father","fault","favor","feast","feature","federal","fee","feed",
        "feel","fellow","female","fence","festival","fetch","fever","fiber",
        "fiction","field","fierce","fight","figure","file","filter","final",
        "finance","find","fine","finger","finish","fire","firm","first",
        "fish","fist","fitness","flag","flame","flash","flask","flat",
        "flavor","flaw","flee","fleet","flesh","flick","flight","flint",
        "float","flock","flood","floor","flour","flow","fluent","fluid",
        "flush","flute","fly","foam","focus","fog","foil","fold",
        "follow","food","fool","foot","force","forest","forget","fork",
        "form","fort","fossil","foster","found","fox","fragile","frame",
        "frank","fraud","free","freeze","fresh","friend","fringe","frog",
        "front","frost","frown","frozen","fruit","fuel","full","fun",
        "fund","funny","fury","future","gadget","gain","galaxy","gallery",
        "game","gamma","gap","garage","garden","garlic","gas","gate",
        "gauge","gaze","gear","gem","gender","general","genius","gentle",
        "genuine","gesture","ghost","giant","gift","giggle","ginger","giraffe",
        "girl","give","glad","glance","glare","glass","glide","glimpse",
        "globe","gloom","glory","glove","glow","glue","goal","goat",
        "gold","golf","gone","good","goose","gorgeous","gossip","govern",
        "grace","grade","grain","grand","grant","grape","graph","grasp",
        "grass","gravity","gray","great","green","greet","grey","grid",
        "grief","grill","grim","grin","grind","grip","groove","ground",
        "group","grow","guard","guess","guest","guide","guilt","guitar",
        "gulf","gum","gun","gust","gutter","gym","habit","hail",
        "hair","half","hall","halo","halt","hammer","hand","handle",
        "handy","hang","happen","happy","harbor","hard","harm","harp",
        "harvest","haste","hat","hatch","hate","haunt","have","hawk",
        "hazard","head","heal","health","heap","hear","heart","heat",
        "heaven","heavy","hedge","heel","height","heir","helicopter","hell",
        "hello","helmet","help","hen","herb","herd","here","hero",
        "hidden","high","hill","hint","hip","hire","history","hit",
        "hobby","hold","hole","holiday","hollow","holy","home","honest",
        "honey","honor","hood","hope","horizon","horn","horror","horse",
        "hospital","host","hotel","hour","house","hover","however","huge",
        "human","humble","humor","hundred","hunt","hurdle","hurry","hurt",
        "husband","hybrid","hydrogen","hyena","hymn","hyphen","ice","icon",
        "idea","ideal","identify","idle","idol","ignite","ignore","ill",
        "image","imagine","impact","imply","import","impose","impress","improve",
        "impulse","inch","include","income","increase","index","indicate","indoor",
        "industry","infant","infect","infinite","influence","inform","inject","injury",
        "ink","inmate","inner","input","inquiry","insect","insert","inside",
        "insist","inspect","inspire","install","instant","instead","instruct","insult",
        "intact","intend","intense","intent","interest","interim","internal","internet",
        "interpret","interrupt","interval","intimate","into","introduce","invade","invent",
        "invest","invite","involve","iron","island","isolate","issue","item",
        "ivory","jack","jacket","jade","jail","jam","jar","jaw",
        "jazz","jealous","jeans","jelly","jet","jewel","job","jockey",
        "join","joke","jolly","journal","journey","joy","judge","judo",
        "juice","jump","jungle","junior","junk","jury","just","justice",
        "keen","keep","kettle","key","kick","kid","kidney","kill",
        "kind","king","kingdom","kiss","kit","kitchen","kite","kitten",
        "knee","knew","knife","knight","knit","knock","knot","know",
        "label","labor","ladder","lady","lake","lamb","lamp","land",
        "lane","language","large","laser","last","late","later","latest",
        "laugh","launch","laundry","law","lawn","lawsuit","layer","lazy",
        "lead","leaf","league","leak","lean","learn","lease","leather",
        "leave","lecture","left","leg","legal","legend","lemon","lend",
        "length","lens","leopard","lesson","let","letter","level","lever",
        "liar","liberty","library","license","lid","lie","life","lift",
        "light","like","limb","limit","line","link","lion","lip",
        "liquid","list","listen","liter","little","live","liver","living",
        "lizard","load","loan","lobby","local","location","lock","locust",
        "logic","logo","lone","long","look","loop","loose","lord",
        "lose","loss","lot","loud","love","low","loyal","luck",
        "luggage","lunch","lung","luxury","lyric","machine","mad","magazine",
        "magic","magnet","maid","mail","main","maintain","major","make",
        "male","mall","mammal","man","manage","mandate","mango","manor",
        "manual","many","map","marble","march","margin","marine","mark",
        "market","marriage","marry","marsh","mask","mass","master","match",
        "mate","matrix","matter","mature","maximum","mayor","maze","meadow",
        "meal","mean","measure","meat","mechanic","medal","media","medical",
        "medicine","medium","meet","melody","melt","member","memo","memory",
        "mental","mention","menu","merchant","mercy","mere","merge","merit",
        "merry","mesh","message","metal","method","middle","midnight","might",
        "mild","mile","milk","mill","million","mind","mine","minimum",
        "minister","minor","mint","minute","miracle","mirror","misery","miss",
        "mistake","mix","mobile","mode","model","moderate","modern","modest",
        "modify","module","moist","moment","money","monitor","monkey","month",
        "mood","moon","moral","more","morning","mortal","mosque","mosquito",
        "most","mother","motion","motor","mount","mountain","mouse","mouth",
        "move","movie","much","mud","muffin","mule","multiple","multiply",
        "murder","muscle","museum","mushroom","music","must","mutual","myself",
        "mystery","myth","nail","name","nap","narrative","narrow","nation",
        "native","nature","navy","near","neat","neck","need","needle",
        "negative","neglect","neighbor","nerve","nest","net","network","neutral",
        "never","new","news","next","nice","niche","niece","night",
        "nine","noble","nobody","node","noise","nomad","none","noon",
        "normal","north","nose","notable","note","nothing","notice","novel",
        "now","nuclear","number","nurse","nut","nylon","oak","oasis",
        "oath","obey","object","oblige","observe","obtain","obvious","occasion",
        "occupy","occur","ocean","october","odd","off","offer","office",
        "officer","official","often","oil","okay","old","olive","omega",
        "omit","once","one","onion","online","only","open","opera",
        "operate","opinion","oppose","optic","option","orange","orbit","order",
        "organ","orient","origin","orphan","ostrich","other","otter","ought",
        "ounce","our","outcome","outdoor","outer","outlet","outline","output",
        "outside","oval","oven","over","overt","owe","own","owner",
        "oxygen","oyster","pace","pack","packet","pact","pad","page",
        "pain","paint","pair","palace","pale","palm","pan","panel",
        "panic","paper","parade","parent","park","parrot","part","party",
        "pass","passion","past","patch","path","patient","patrol","pattern",
        "pause","pave","pawn","pay","peace","peak","peanut","pear",
        "pearl","pedal","peel","peer","pen","penalty","pencil","people",
        "pepper","perceive","perfect","perform","perfume","period","permit","person",
        "pet","phase","phone","photo","phrase","physical","piano","pick",
        "picture","piece","pier","pig","pigeon","pile","pill","pilot",
        "pin","pine","pink","pipe","pistol","pitch","pity","pizza",
        "place","plain","plan","plane","planet","plant","plastic","plate",
        "platform","play","plaza","plea","please","pledge","plenty","plot",
        "plow","plug","plunge","plus","pocket","poem","poet","point",
        "poison","polar","pole","police","policy","polite","political","poll",
        "polo","pond","pony","pool","poor","pop","popular","pork",
        "port","pose","position","positive","possible","post","pot","potato",
        "pound","pour","powder","power","practice","praise","pray","preach",
        "precious","predict","prefer","prefix","prepare","present","press","pretty",
        "prevent","prey","price","pride","primary","prince","print","prior",
        "prison","private","prize","probe","problem","process","produce","profit",
        "program","project","promote","proof","proper","prose","protect","protein",
        "protest","proud","prove","provide","proxy","public","pudding","pull",
        "pulse","pump","punch","pupil","puppy","purchase","pure","purple",
        "purpose","purse","push","put","puzzle","pyramid","quake","quality",
        "quantum","quarter","queen","quest","quick","quiet","quilt","quit",
        "quiz","quota","quote","rabbit","race","rack","radar","radio",
        "raft","rage","raid","rail","rain","raise","rally","ranch",
        "random","range","rank","rapid","rare","rat","rate","ratio",
        "raven","raw","ray","razor","reach","react","read","ready",
        "real","realm","reap","rear","reason","rebel","recall","receive",
        "recipe","reckon","record","recover","recruit","red","redeem","reduce",
        "reef","refer","refine","reflect","reform","refuge","refuse","regard",
        "regime","region","regret","regular","reign","reject","relate","relax",
        "relay","release","relief","rely","remain","remark","remedy","remind",
        "remote","remove","render","renew","rent","repair","repeat","replace",
        "reply","report","rescue","research","resemble","reserve","reset","reside",
        "resign","resist","resolve","resort","resource","respect","respond","rest",
        "result","retain","retire","retreat","return","reveal","revenge","revenue",
        "review","revive","reward","rhythm","rib","ribbon","rice","rich",
        "rid","ride","ridge","rifle","right","rigid","ring","riot",
        "rise","risk","ritual","rival","river","road","roar","roast",
        "rob","robot","robust","rock","rod","role","roll","roof",
        "room","root","rope","rose","rotate","rough","round","route",
        "routine","royal","rubber","ruby","rug","rule","ruler","rumor",
        "run","rural","rush","rust","sack","sacred","sad","safe",
        "safety","saga","sail","saint","sake","salad","salary","sale",
        "salmon","salon","salt","salute","same","sample","sand","sandal",
        "sandwich","sane","sarcasm","satisfy","sauce","sausage","save","savior",
        "saw","say","scale","scan","scare","scarf","scene","scent",
        "schedule","scheme","school","science","scoop","scope","score","scout",
        "scrape","scratch","scream","screen","script","scroll","scrub","sea",
        "seal","search","season","seat","second","secret","section","sector",
        "secure","see","seed","seek","seem","segment","seize","select",
        "self","sell","send","senior","sense","sentence","separate","sequence",
        "serene","series","serious","serpent","serve","service","session","set",
        "settle","seven","sever","severe","sew","shade","shadow","shaft",
        "shake","shallow","shame","shape","share","shark","sharp","shave",
        "sheep","sheet","shelf","shell","shelter","shield","shift","shine",
        "ship","shirt","shock","shoe","shoot","shop","shore","short",
        "shot","should","shout","show","shower","shrug","shut","shy",
        "sick","side","siege","sight","sign","signal","silence","silent",
        "silk","silver","similar","simple","since","sing","single","sink",
        "sister","sit","site","situation","six","size","skate","sketch",
        "ski","skill","skin","skip","skirt","skull","sky","slab",
        "slam","slap","slash","slave","sleep","slice","slide","slight",
        "slim","slip","slogan","slope","slot","slow","slug","slum",
        "sly","small","smart","smash","smell","smile","smoke","smooth",
        "snack","snake","snap","snow","soak","soap","soar","soccer",
        "social","sock","soda","sofa","soft","soil","solar","soldier",
        "solid","solo","solve","some","son","song","soon","sore",
        "sorrow","sort","soul","sound","soup","source","south","space",
        "spare","spark","sparrow","speak","spear","special","speech","speed",
        "spell","spend","sphere","spider","spike","spill","spin","spine",
        "spirit","spit","splash","split","spoil","sponge","spoon","sport",
        "spot","spouse","spray","spread","spring","spy","squad","square",
        "squash","squid","squirrel","stable","stack","staff","stage","stain",
        "stair","stake","stall","stamp","stand","star","stare","start",
        "starve","state","static","station","statue","status","stay","steady",
        "steak","steal","steam","steel","steep","steer","stem","step",
        "stereo","stick","still","sting","stir","stock","stomach","stone",
        "stool","stop","store","storm","story","stove","straight","strain",
        "strange","strap","straw","stray","stream","street","stress","stretch",
        "strict","strike","string","strip","strive","stroke","strong","structure",
        "struggle","student","studio","study","stuff","stumble","stun","stunt",
        "style","subject","submit","subway","success","such","sudden","suffer",
        "sugar","suggest","suit","summer","summit","sun","super","supply",
        "support","supreme","sure","surface","surge","surgeon","surgery","surplus",
        "surprise","surrender","survey","survive","suspect","sustain","swallow","swamp",
        "swan","swarm","swear","sweat","sweep","sweet","swell","swift",
        "swim","swing","switch","sword","symbol","symptom","syndrome","system",
        "table","tablet","tackle","tactic","tail","take","tale","talent",
        "talk","tall","tame","tan","tank","tap","tape","target",
        "task","taste","tattoo","taxi","tea","teach","team","tear",
        "tease","technical","technology","teen","telephone","telescope","tell","temper",
        "temple","tempo","tempt","tenant","tend","tender","tennis","tent",
        "term","terminal","terrace","terrain","terrible","terror","test","text",
        "texture","than","thank","that","theater","theme","then","theory",
        "therapy","there","thermal","thick","thief","thigh","thin","thing",
        "think","third","thirst","thirty","this","thorn","those","thought",
        "thread","threat","three","thrift","thrill","thrive","throat","throne",
        "through","throw","thrust","thumb","thunder","thus","tick","ticket",
        "tide","tidy","tie","tiger","tight","tile","till","tilt",
        "timber","time","tin","tiny","tip","tire","tissue","title",
        "toast","tobacco","today","toe","together","toilet","token","tolerate",
        "toll","tomato","tomb","tone","tongue","tonight","too","tool",
        "tooth","top","topic","torch","tornado","tortoise","toss","total",
        "touch","tough","tour","toward","towel","tower","town","toxic",
        "toy","trace","track","trade","traffic","tragic","trail","train",
        "trait","tram","trance","transfer","trap","trash","travel","tray",
        "treasure","treat","tree","trend","trial","tribe","trick","trip",
        "troop","trophy","trouble","truck","true","trumpet","trunk","trust",
        "truth","try","tube","tuition","tulip","tuna","tune","tunnel",
        "turkey","turn","turtle","tutor","twin","twist","type","typical",
        "ugly","ultra","umbrella","unable","uncle","under","undo","unfair",
        "unfold","unhappy","uniform","union","unique","unit","unite","unity",
        "universe","university","unknown","unless","unlike","until","unusual","update",
        "upgrade","uphold","upon","upper","upset","urban","urge","urgent",
        "use","usual","utility","utter","vacant","vacation","vacuum","vague",
        "valid","valley","value","valve","van","vanish","vapor","variable",
        "variant","variety","various","vault","vector","vegetable","vehicle","veil",
        "vein","velocity","velvet","vendor","venture","verb","verdict","verify",
        "version","vertical","very","vessel","veteran","veto","viable","vibrant",
        "victim","victory","video","view","village","vintage","violin","virtual",
        "virtue","virus","visa","visible","vision","visit","visual","vital",
        "vitamin","vivid","vocal","voice","void","volcano","volume","volunteer",
        "vote","vowel","voyage","vulture","wage","wagon","waist","wait",
        "wake","walk","wall","wander","want","war","ward","warm",
        "warn","warp","warrior","wash","wasp","waste","watch","water",
        "wave","wax","way","weak","wealth","weapon","wear","weasel",
        "weather","weave","wedding","weed","week","weird","welcome","well",
        "west","wet","whale","what","wheat","wheel","when","where",
        "whether","which","while","whip","whisper","whistle","white","who",
        "whole","why","wicked","wide","widow","width","wife","wild",
        "will","win","wind","window","wine","wing","wink","winner",
        "winter","wire","wisdom","wise","wish","wit","witch","with",
        "withdraw","within","without","witness","wolf","woman","wonder","wood",
        "wool","word","work","world","worm","worry","worse","worth",
        "would","wound","wrap","wreck","wrestle","wrist","write","wrong",
        "yard","yarn","year","yeast","yellow","yes","yesterday","yield",
        "yoga","young","youth","zebra","zero","zone","zoo","zoom"
    };

    public string Generate(int length, bool useUppercase, bool useLowercase, bool useDigits, bool useSymbols, bool excludeAmbiguous)
    {
        if (length < 4 || length > 128)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 4 and 128.");

        var sets = new List<string>(4);
        if (useLowercase) AddSet(sets, Lowercase, excludeAmbiguous);
        if (useUppercase) AddSet(sets, Uppercase, excludeAmbiguous);
        if (useDigits) AddSet(sets, Digits, excludeAmbiguous);
        if (useSymbols) AddSet(sets, Symbols, excludeAmbiguous);

        if (sets.Count == 0)
            throw new InvalidOperationException("Select at least one character set.");

        var result = new char[length];
        int index = 0;

        // Guarantee at least one character from each selected set.
        foreach (string set in sets)
        {
            if (index >= length) break;
            result[index++] = Pick(set);
        }

        string pool = string.Concat(sets);
        while (index < length)
            result[index++] = Pick(pool);

        Shuffle(result);
        return new string(result);
    }

    public string GenerateRecoveryPhrase(int wordCount = 12)
    {
        if (wordCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(wordCount));

        return string.Join(" ", PickWords(wordCount));
    }

    public string GeneratePassphrase(int wordCount, string separator = "-")
    {
        if (wordCount is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(wordCount));

        return string.Join(separator, PickWords(wordCount));
    }

    public int CharacterPoolSize(bool useUppercase, bool useLowercase, bool useDigits, bool useSymbols, bool excludeAmbiguous)
    {
        int size = 0;
        if (useLowercase) size += EffectiveSetSize(Lowercase, excludeAmbiguous);
        if (useUppercase) size += EffectiveSetSize(Uppercase, excludeAmbiguous);
        if (useDigits) size += EffectiveSetSize(Digits, excludeAmbiguous);
        if (useSymbols) size += EffectiveSetSize(Symbols, excludeAmbiguous);
        return size;
    }

    public int WordListSize => RecoveryWords.Length;

    public double CalculateEntropy(int length, int poolSize)
    {
        if (poolSize <= 1) return 0;
        return length * Math.Log2(poolSize);
    }

    public double CalculatePassphraseEntropy(int wordCount)
    {
        return wordCount * Math.Log2(RecoveryWords.Length);
    }

    public static string StrengthLabel(double entropy) => entropy switch
    {
        < 40 => "Weak",
        < 60 => "Fair",
        < 80 => "Good",
        < 100 => "Strong",
        _ => "Very strong"
    };

    private static List<string> PickWords(int count)
    {
        var words = new List<string>(count);
        for (int i = 0; i < count; i++)
            words.Add(RecoveryWords[RandomNumberGenerator.GetInt32(RecoveryWords.Length)]);
        return words;
    }

    private static char Pick(string pool) => pool[RandomNumberGenerator.GetInt32(pool.Length)];

    private static void Shuffle<T>(T[] items)
    {
        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static void AddSet(List<string> sets, string set, bool excludeAmbiguous)
    {
        string effective = excludeAmbiguous ? RemoveAmbiguous(set) : set;
        if (effective.Length > 0) sets.Add(effective);
    }

    private static int EffectiveSetSize(string set, bool excludeAmbiguous)
        => excludeAmbiguous ? RemoveAmbiguous(set).Length : set.Length;

    private static string RemoveAmbiguous(string set)
    {
        var builder = new StringBuilder(set.Length);
        foreach (char c in set)
        {
            if (!Ambiguous.Contains(c)) builder.Append(c);
        }
        return builder.ToString();
    }
}
