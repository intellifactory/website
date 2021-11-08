namespace Online

open System
open System.IO
open System.Xml.Linq
open System.Text.RegularExpressions
open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Server
open WebSharper.UI.Templating

type EndPoint =
    | [<EndPoint "/">] Home
    // The main blog page
    | [<EndPoint "GET /blogs">] Blogs
    // User-less blog articles
    | [<EndPoint "GET /post">] Article of slug:string
    // UserArticle: if slug is empty, we go to the user's home page
    | [<EndPoint "GET /user">] UserArticle of user:string * slug:string
    | [<EndPoint "GET /refresh">] Refresh
    | [<EndPoint "GET /feed.atom">] AtomFeed
    | [<EndPoint "GET /feed.rss">] RSSFeed
    | [<EndPoint "GET /atom">] AtomFeedForUser of string
    | [<EndPoint "GET /rss">] RSSFeedForUser of string
    | [<EndPoint "GET /contact">] Contact

type PostTemplate = Template<"../Online/post.html", serverLoad=ServerLoad.WhenChanged>
type ContactTemplate = Template<"../Online/contact.html", serverLoad=ServerLoad.WhenChanged>
type BlogsTemplate = Template<"../Online/blogs.html", serverLoad=ServerLoad.WhenChanged>

// Utilities to make XML construction somewhat sane
[<AutoOpen>]
module Xml =
    let TEXT (s: string) = XText(s)
    let (=>) (a1: string) (a2: string) = XAttribute(XName.Get a1, a2)
    let N = XName.Get
    let X (tag: XName) (attrs: XAttribute list) (content: obj list) =
        XElement(tag, List.map box attrs @ List.map box content)

module Markdown =
    open Markdig

    let pipeline =
        MarkdownPipelineBuilder()
            .UseAutoIdentifiers()
            .UsePipeTables()
            .UseFootnotes()
            .UseGridTables()
            .UseListExtras()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseTaskLists()
            .UseMediaLinks()
            .UseCustomContainers()
            .UseGenericAttributes()
            .UseMathematics()
            .UseEmojiAndSmiley()
            .UseYamlFrontMatter()
            .UseAdvancedExtensions()
            .Build()

    let Convert (content: string) = Markdown.ToHtml(content, pipeline)

module Yaml =
    open System.Text.RegularExpressions
    open YamlDotNet.Serialization

    let SplitIntoHeaderAndContent (source: string) =
        let delimRE = Regex("^---\\w*\r?$", RegexOptions.Compiled ||| RegexOptions.Multiline)
        let searchFrom = if source.StartsWith("---") then 3 else 0
        let m = delimRE.Match(source, searchFrom)
        if m.Success then
            source.[searchFrom..m.Index-1], source.[m.Index + m.Length..]
        else
            "", source

    let OfYaml<'T> (yaml: string) =
        let deserializer = (new DeserializerBuilder()).Build()
        if String.IsNullOrWhiteSpace yaml then
            deserializer.Deserialize<'T>("{}")
        else
            let yaml = deserializer.Deserialize<'T>(yaml)
            eprintfn "DEBUG/YAML=%A" yaml
            yaml

// Helpers around blog URLs.
// These need to match the endpoint type of the main sitelet.
module Urls =
    let CATEGORY (cat: string) lang =
        if String.IsNullOrEmpty lang then
            sprintf "/category/%s" cat
        else
            sprintf "/category/%s/%s" cat lang
    let POST_URL (user: string, slug: string) =
        if String.IsNullOrEmpty user then
            sprintf "/post/%s.html" slug
        else
            sprintf "/user/%s/%s" user slug
    let OLD_TO_POST_URL (user: string, datestring: string, oldslug: string) =
        POST_URL (user, sprintf "%s-%s" datestring oldslug)
    let USER_URL user =
        if String.IsNullOrEmpty user then
            sprintf "/user"
        else
            sprintf "/user/%s" user
    let LANG (lang: string) = sprintf "/%s" lang
    let RSS_URL user =
        if String.IsNullOrEmpty user then
            sprintf "/rss"
        else
            sprintf "/rss/%s.rss" user

module Helpers =
    open System.IO
    open System.Text.RegularExpressions

    let NULL_TO_EMPTY (s: string) = match s with null -> "" | t -> t

    let FORMATTED_DATE (dt: DateTime) = dt.ToString("MMM dd, yyyy")
    let ATOM_DATE (dt: DateTime) = dt.ToString("yyyy-MM-dd'T'HH:mm:ssZ")
    let RSS_DATE (dt: DateTime) = dt.ToString("ddd, dd MMM yyyy HH:mm:ss UTC")

    // Return (fullpath, filename-without-extension, (year, month, day), slug, extension)
    let (|ArticleFile|_|) (fullpath: string) =
        let I s = Int32.Parse s
        let filename = Path.GetFileName(fullpath)
        let filenameWithoutExt = Path.GetFileNameWithoutExtension(fullpath)
        let r = new Regex("^(([0-9]+)-([0-9]+)-([0-9]+))-(.+)\.(md)")
        let r2 = new Regex("^(([1-2][0-9][0-9][0-9])([0-1][0-9])([0-3][0-9]))-(.+)\.(md)")
        if r.IsMatch(filename) then
            let a = r.Match(filename)
            let V (i: int) = a.Groups.[i].Value
            Some (fullpath, filenameWithoutExt, V 1, (I (V 2), I (V 3), I (V 4)), V 5, V 6)
        elif r2.IsMatch(filename) then
            let a = r2.Match(filename)
            let V (i: int) = a.Groups.[i].Value
            Some (fullpath, filenameWithoutExt, V 1, (I (V 2), I (V 3), I (V 4)), V 5, V 6)
        else
            None

    let (|NUMBER|_|) (s: string) =
        let out = ref 0
        if Int32.TryParse(s, out) then Some !out else None

module Site =
    type [<CLIMutable>] RawConfig =
        {
            serverUrl: string
            shortTitle: string
            title: string
            description: string
            masterUserDisplayName: string
            masterLanguage: string
            languages: string
            users: string
            pageSize: int
            categoriesCountIfNotAll: int
            githubRepo: string
        }

    type Config =
        {
            ServerUrl: string
            ShortTitle: string
            Title: string
            Description: string
            MasterUserDisplayName: string
            MasterLanguage: string
            Languages: Map<string, string>
            Users: Map<string, string>
            PageSize: int
            CategoriesCountIfNotAll: int
            GitHubRepo: string
        }

    type [<CLIMutable>] RawArticle =
        {
            title: string
            subtitle: string
            ``abstract``: string
            url: string
            content: string
            date: string
            categories: string
            language: string
            identity: string
        }

    type Article =
        {
            Title: string
            Subtitle: string
            Abstract: string
            AuthorName: string
            User: string
            Url: string
            AuthorUrl: string
            Content: string
            DateString: string
            SlugWithoutDate: string
            Date: DateTime
            Categories: string list
            CategoryNumber: int
            Language: string
            Identity: int * int
            TimeToRead: float
        }

    type BlogInfoRaw =
        {
            YearsActive: Map<string, Set<int>>
            Categories: Map<string, int>
            Languages: string list
        }

        static member Empty =
            {
                YearsActive = Map.empty
                Categories = Map.empty
                Languages = []
            }

    // The article store, mapping (user*slug) pairs to articles.
    type Articles = Map<string*string, Article>

    // Mapping Id1 -> (username, datestring)
    type Identities1 = Map<int, string*string>

    /// Zero out if article has the master language
    let URL_LANG (config: Config) lang =
        if config.MasterLanguage = lang then "" else lang

    let ReadConfig() =
        let KEY_VALUE_LIST whatFor (ss: string) =
            (Helpers.NULL_TO_EMPTY ss).Split([| "," |], StringSplitOptions.None)
            |> Array.choose (fun s ->
                if String.IsNullOrEmpty s then
                    None
                else
                    let parts = s.Split([| "->" |], StringSplitOptions.None)
                    if Array.length parts <> 2 then
                        eprintfn "warning: Incorrect key-value format for substring [%s] in [%s] for [%s], ignoring." s ss whatFor
                        None
                    else
                        Some (parts.[0], parts.[1])
            )
            |> Set.ofArray
            |> Set.toList
            |> Map.ofList
        let config = Path.Combine (__SOURCE_DIRECTORY__, @"../Online/config.yml")
        if File.Exists config then
            let config = Yaml.OfYaml<RawConfig> (File.ReadAllText config)
            let languages = KEY_VALUE_LIST "languages" config.languages
            let users = KEY_VALUE_LIST "users" config.users
            {
                ServerUrl = Helpers.NULL_TO_EMPTY config.serverUrl
                ShortTitle = Helpers.NULL_TO_EMPTY config.shortTitle
                Title = Helpers.NULL_TO_EMPTY config.title
                Description = Helpers.NULL_TO_EMPTY config.description
                MasterUserDisplayName = Helpers.NULL_TO_EMPTY config.masterUserDisplayName
                MasterLanguage = Helpers.NULL_TO_EMPTY config.masterLanguage
                Languages = languages
                Users = users
                PageSize = if config.pageSize > 0 then config.pageSize else 30
                CategoriesCountIfNotAll = if config.categoriesCountIfNotAll > 0 then config.categoriesCountIfNotAll else 30
                GitHubRepo = Helpers.NULL_TO_EMPTY config.githubRepo
            }
        else
            {
                ServerUrl = "http://localhost:5000"
                ShortTitle = "My Blog"
                Title = "My F# Blog"
                Description = "TODO: write the description of this blog"
                MasterUserDisplayName = "My Name"
                MasterLanguage = "en"
                Languages = Map.ofList ["en", "English"]
                Users = Map.empty
                PageSize = 30
                CategoriesCountIfNotAll = 30
                GitHubRepo = "https://github.com/IntelliFactory/blogs"
            }

    let ReadArticles (config: Config) : BlogInfoRaw * Articles =
        let root = Path.Combine (__SOURCE_DIRECTORY__, @"../Online/posts")
        let ReadFolder user store =
            let folder = Path.Combine (root, user)
            if Directory.Exists folder then
                Directory.EnumerateFiles(folder, "*.md", SearchOption.TopDirectoryOnly)
                |> Seq.toList
                |> List.choose (Helpers.(|ArticleFile|_|))
                |> List.fold (fun map (fullpath, fname, datestring, (year, month, day), slug, extension) ->
                    eprintfn "Found file: %s" fname
                    let header, content =
                        File.ReadAllText fullpath
                        |> Yaml.SplitIntoHeaderAndContent
                    let article = Yaml.OfYaml<RawArticle> header
                    let title = Helpers.NULL_TO_EMPTY article.title
                    let subtitle = Helpers.NULL_TO_EMPTY article.subtitle
                    let ``abstract`` = Helpers.NULL_TO_EMPTY article.``abstract``
                    let url = Urls.POST_URL (user, fname)
                    eprintfn "DEBUG-URL: %s" url
                    // Process article content.
                    // We make a couple small adjustments:
                    //  1) Relative URLs in the form `/user/*/*.md` are converted to point to *.html.
                    //     This is to preserve cross-links between IF articles in the repo and the live site.
                    //  2) Relative URLs in the form `/asset/*` are converted to point to GitHub.
                    //     This is to serve embedded artifacts from there directly.
                    let content =
                        // If the content is given in the header, use that instead.
                        let content =
                            if article.content <> null then
                                article.content
                            else
                                content
                        // 1)
                        let content = Regex.Replace(content, "\(\s*(\/user\/[^\/]+\/[^\.]*)\.md\s*\)", "($1.html)")
                        // 2)
                        let content = Regex.Replace(content, "\(\s*(\/assets\/[^\s]*)\s*\)", "(" + config.GitHubRepo + "/raw/master$1)")
                        Markdown.Convert content
                    let date = DateTime(year, month, day)
                    let categories =
                        Helpers.NULL_TO_EMPTY article.categories
                    // Clean up article tags/categories:
                    let categories =
                        if not <| String.IsNullOrEmpty categories then
                            categories.Split [| ',' |]
                            // Note: categories are case-sensitive.
                            // Trim each and convert the "#" character - so "c/f#" becomes "c/fsharp" 
                            |> Array.map (fun cat -> cat.Trim().Replace("#", "sharp"))
                            |> Array.filter (not << String.IsNullOrEmpty)
                            |> Set.ofArray
                            |> Set.toList
                        else
                            []
                    // Assign a category to each article
                    // 0 - General
                    // 1 - Bolero
                    // 2 - CloudSharper
                    // 3 - WebSharper
                    // 4 - Blogging/SiteFi
                    // 5 - Release announcement
                    // 6 - TryWebSharper
                    let categoryNo =
                        let cats = List.map (fun (c: string) -> c.ToLower()) categories
                        let rn = new Regex("^(Bolero|WebSharper|CloudSharper|SiteFi)\s[0-9\.]+\srelease")
                        if rn.IsMatch(title) then
                            5
                        elif List.contains "bolero" cats then
                            1
                        elif List.contains "cloudsharper" cats then
                            2
                        elif List.contains "blogging" cats || List.contains "sitefi" cats then
                            4
                        elif List.contains "trywebsharper" cats || List.contains "tryws" cats then
                            6
                        elif List.contains "websharper" cats then
                            3
                        else
                            0
                    let language = Helpers.NULL_TO_EMPTY article.language
                    let identity =
                        let raw = Helpers.NULL_TO_EMPTY article.identity
                        let entries = raw.Split([| ',' |])
                        match entries with
                        | [| Helpers.NUMBER id1; Helpers.NUMBER id2 |] ->
                            id1, id2
                        | _ ->
                            failwithf "Invalid identity found (%A)" entries
                    let timeToRead =
                        let words = content.Split([|' '|]).Length
                        Math.Ceiling(float words / 200.) // Avarage WPM is 200
                    eprintfn "DEBUG-ADD: (%s, %s)\n-------------------" user fname
                    Map.add (user, fname)
                        {
                            Title = title
                            Subtitle = subtitle
                            Abstract = ``abstract``
                            AuthorName = if config.Users.ContainsKey user then config.Users.[user] else user
                            User = user
                            Url = url
                            AuthorUrl = Urls.USER_URL user
                            Content = content
                            DateString = datestring
                            SlugWithoutDate = slug
                            Date = date
                            Categories = categories
                            CategoryNumber = categoryNo
                            Language = language
                            Identity = identity
                            TimeToRead = timeToRead
                        } map
                ) store
            else
                eprintfn "warning: the posts folder (%s) does not exist." folder
                store
        
        let articles =
            Directory.EnumerateDirectories(root)
            // Read user articles
            |> Seq.fold (fun store folder ->
                ReadFolder (Path.GetFileName(folder)) store) Map.empty
            // Read main articles
            |> ReadFolder ""
        let info =
            let ADD_TO_SET (map: Map<'T, Set<'U>>) (k: 'T) (v: 'U) =
                let newValue =
                    if Map.containsKey k map then
                        Set.add v (map.[k])
                    else
                        Set.singleton v
                Map.add k newValue map
            let ADD_TO_SUM (map: Map<'T, int>) (k: 'T) =
                let newValue =
                    if Map.containsKey k map then
                        map.[k] + 1
                    else
                        1
                Map.add k newValue map
            let yearsActive =
                articles
                |> Map.fold (fun info (user, _) article ->
                    ADD_TO_SET info user article.Date.Year
                ) Map.empty
                |> Map.fold (fun info user years ->
                    Map.add user years info
                ) Map.empty
            let categories =
                articles
                |> Map.fold (fun info (user, _) article ->
                    article.Categories
                    |> List.fold (fun info category ->
                        ADD_TO_SUM info category
                    ) info
                ) Map.empty
            let languages =
                articles
                |> Map.toList
                |> List.map snd
                |> List.map (fun article -> URL_LANG config article.Language)
                |> Set.ofList
                |> Set.toList
            {
                YearsActive = yearsActive
                Categories = categories
                Languages = languages
            }
        info, articles

    // Here we map the Id1 -> (user, datestring).
    let ComputeIdentities1 (articles: Articles) : Identities1 =
        articles
        |> Map.fold (fun map (user, _) article ->
            Map.add (fst article.Identity) (user, article.DateString) map
        ) Map.empty

    let PLAIN html =
        div [Attr.Create "ws-preserve" ""] [Doc.Verbatim html]

    let private head() =
        __SOURCE_DIRECTORY__ + "/../Online/wwwroot/js/Dynamic.head.html"
        |> File.ReadAllText
        |> Doc.Verbatim

    let ArticlePage (config: Config) articles (article: Article) =
        let head = head()
        PostTemplate()
#if !DEBUG
            .ReleaseMin(".min")
#endif
            .Head(head)
            .MenubarPlaceholder(
                PostTemplate.Menubar()
                    .Doc()
            )
//            .Cookie(Cookies.Banner false)
            .FooterPlaceholder(PostTemplate.Footer().Doc())
            // Main content panel
            .Content(
                PLAIN article.Content
            )
            .TimeToRead(string article.TimeToRead)
//            .SourceCodeUrl(sprintf "%s/tree/master%s.md" config.GitHubRepo article.Url)
            .Date(article.Date.ToString("MMM dd, yyyy"))
            .Title(article.Title)
//            .Description(article.Abstract)
//            .PageUrl(article.Url)
            .AuthorName(article.AuthorName)
            .AuthorUsernameForAvatar(
                let fname = Path.Combine (__SOURCE_DIRECTORY__, sprintf @"../Online/wwwroot/img/avatar/%s.png" article.User)
                if File.Exists(fname) then
                    article.User
                else
                    "user"
            )
//            .AuthorUrl(article.AuthorUrl)
            .CategoryNo(string article.CategoryNumber)
//            .ServerUrl(config.ServerUrl)
            // Sidebar
//            .Sidebar(BlogSidebar config articles article)
            .Doc()
          |> Content.Page

    // The silly ref's are needed because offline sitelets are
    // initialized in their own special way, without having access
    // to top-level values.
    let __info : BlogInfoRaw ref = ref BlogInfoRaw.Empty
    let __articles : Articles ref = ref Map.empty
    let __identities1 : Identities1 ref = ref Map.empty
    let __config : Config ref = ref <| ReadConfig()

    [<Website>]
    let Main (config: Config ref) (identities1: Identities1 ref) (info: BlogInfoRaw ref) (articles: Articles ref) =
        Application.MultiPage (fun ctx endpoint ->
            let BLOGS ctx =
                BlogsTemplate()
                    .MenubarPlaceholder(PostTemplate.Menubar().Doc())
                    .FooterPlaceholder(PostTemplate.Footer().Doc())
                    .Articles(
                        BlogsTemplate.ArticlesSection()
                            .Articles(
                                (!articles)
                                |> Map.toList
                                |> List.map snd
                                |> List.sortBy (fun art -> -art.Date.Ticks)
                                |> List.map (fun art ->
                                    BlogsTemplate.ArticleBlock()
                                        .AuthorName(art.AuthorName)
                                        .Date(art.DateString)
                                        .Title(art.Title)
                                        .AuthorThumbnailUrl(sprintf "/img/avatar/%s.png" art.User)
                                        .MinutesToRead(string (int (Math.Ceiling art.TimeToRead)))
                                        .Doc()
                                )
                                |> Doc.Concat
                            )
                            .Doc()
                    )
                    .Doc()
                |> Content.Page
            let ARTICLE (user, p: string) =
                let page =
                    if p.ToLower().EndsWith(".html") then
                        p.Substring(0, p.Length-5)
                    else
                        p
                let key = user, page
                if articles.Value.ContainsKey key then
                    ArticlePage config.Value articles.Value articles.Value.[key]
                else
                    Map.toList articles.Value
                    |> List.map fst
                    |> sprintf "Trying to find page \"%s\" (with key=\"%s\"), but it's not in %A" p page
                    |> Content.Text
            let ARTICLES_BY_USEROPT (userOpt: string option) =
                articles.Value |> Map.toList
                // Filter by user, if given
                |> List.filter (fun ((user, _), _) -> if userOpt.IsSome then user = userOpt.Value else true)
                |> List.sortByDescending (fun (_, article: Article) -> article.Date.Ticks)
            let ATOM_FEED userOpt =
                let ns = XNamespace.Get "http://www.w3.org/2005/Atom"
                let articles = ARTICLES_BY_USEROPT userOpt
                X (ns + "feed") [] [
                    X (ns + "title") [] [TEXT config.Value.Title]
                    X (ns + "subtitle") [] [TEXT config.Value.Description]
                    X (ns + "link") ["href" => config.Value.ServerUrl] []
                    X (ns + "updated") [] [Helpers.ATOM_DATE DateTime.UtcNow]
                    for ((user, slug), article) in articles do
                        X (ns + "entry") [] [
                            X (ns + "title") [] [TEXT article.Title]
                            X (ns + "link") ["href" => config.Value.ServerUrl + Urls.POST_URL (user, slug)] []
                            X (ns + "id") [] [TEXT (user+slug)]
                            for category in article.Categories do
                                X (ns + "category") [] [TEXT category]
                            X (ns + "summary") [] [TEXT article.Abstract]
                            X (ns + "updated") [] [TEXT <| Helpers.ATOM_DATE article.Date]
                        ]
                ]
            let RSS_FEED userOpt =
                let articles = ARTICLES_BY_USEROPT userOpt
                X (N "rss") ["version" => "2.0"] [
                    X (N "channel") [] [
                        X (N "title") [] [TEXT config.Value.Title]
                        X (N "description") [] [TEXT config.Value.Description]
                        X (N "link") [] [TEXT config.Value.ServerUrl]
                        X (N "lastBuildDate") [] [Helpers.RSS_DATE DateTime.UtcNow]
                        for ((user, slug), article) in articles do
                            X (N "item") [] [
                                X (N "title") [] [TEXT article.Title]
                                X (N "link") [] [TEXT <| config.Value.ServerUrl + Urls.POST_URL (user, slug)]
                                X (N "guid") ["isPermaLink" => "false"] [TEXT (user+slug)]
                                for category in article.Categories do
                                    X (N "category") [] [TEXT category]
                                X (N "description") [] [TEXT article.Abstract]
                                X (N "pubDate") [] [TEXT <| Helpers.RSS_DATE article.Date]
                            ]
                    ]
                ]
            let CONTACT () =
//                let mapContactStyles = mapContactStyles()
                ContactTemplate()
#if !DEBUG
                    .ReleaseMin(".min")
#endif
                    .MenubarPlaceholder(PostTemplate.Menubar().Doc())
//                    .Map(client <@ ClientSideCode.TalksAndPresentations.GMapOffice(mapContactStyles) @>)
                    .FooterPlaceholder(PostTemplate.Footer().Doc())
//                    .Cookie(Cookies.Banner false)
                    .Doc()
                |> Content.Page

            match endpoint with
            | EndPoint.Home ->
                Content.Text "Home"
            | Blogs ->
                BLOGS ctx
            | Article p ->
                ARTICLE ("", p)
            // All articles by a given user
            | UserArticle (user, "") ->
                Content.Text "Not implemented"
                //USERBLOG_LISTING_NO_PAGING user
                //    <| fun (u, _) _ -> user = u
            | UserArticle (user, p) ->
                ARTICLE (user, p)
            | AtomFeed ->
                Content.Custom (
                    Status = Http.Status.Ok,
                    Headers = [Http.Header.Custom "content-type" "application/atom+xml"],
                    WriteBody = fun stream ->
                        let doc = ATOM_FEED None
                        doc.Save(stream)
                )
            | AtomFeedForUser user ->
                Content.Custom (
                    Status = Http.Status.Ok,
                    Headers = [Http.Header.Custom "content-type" "application/atom+xml"],
                    WriteBody = fun stream ->
                        let doc = ATOM_FEED (Some user)
                        doc.Save(stream)
                )
            | RSSFeed ->
                Content.Custom (
                    Status = Http.Status.Ok,
                    Headers = [Http.Header.Custom "content-type" "application/rss+xml"],
                    WriteBody = fun stream ->
                        let doc = RSS_FEED None
                        doc.Save(stream)
                )
            | RSSFeedForUser user ->
                Content.Custom (
                    Status = Http.Status.Ok,
                    Headers = [Http.Header.Custom "content-type" "application/rss+xml"],
                    WriteBody = fun stream ->
                        let doc = RSS_FEED (Some user)
                        doc.Save(stream)
                )
            | Refresh ->
                // Reload the master configs and the article cache
                config := ReadConfig()
                let _info, _articles = ReadArticles (!config)
                info := _info
                articles := _articles
                identities1 := ComputeIdentities1 articles.Value
                Content.Text "Articles/configs reloaded."
            | Contact ->
                CONTACT ()
        )

open System.IO

[<Sealed>]
type Website() =
    let config = ref <| Site.ReadConfig()
    let _info, _articles = Site.ReadArticles (!config)
    let info = ref _info
    let articles = ref _articles
    let identities1 = ref <| Site.ComputeIdentities1 articles.Value

    interface IWebsite<EndPoint> with
        member this.Sitelet = Site.Main config identities1 info articles
        member this.Actions =
            let articles = Map.toList articles.Value
            let categories =
                articles
                |> List.map snd
                |> List.collect (fun article -> article.Categories)
                |> Set.ofList
                |> Set.toList
            let languages =
                articles
                |> List.map snd
                |> List.map (fun article -> Site.URL_LANG config.Value article.Language)
                |> Set.ofList
                |> Set.toList
            let noPagesForLanguage language =
                let noArticlesInLanguage =
                    articles
                    |> List.map snd
                    |> List.filter (fun article -> language = Site.URL_LANG config.Value article.Language)
                    |> List.length
                noArticlesInLanguage / config.Value.PageSize + 1
            let users =
                articles
                |> List.map (fst >> fst)
                |> Set.ofList
                |> Set.toList
            //let jobs =
            //    DirectoryInfo("../Hosted/jobs/").EnumerateFiles("*.html", SearchOption.TopDirectoryOnly)
            //    |> Seq.map (fun x -> x.Name.Replace(".html", ""))
            //    |> List.ofSeq
            [
                //// Generate the learning page
                //for training in Site.trainings do
                //    Courses training
                //Trainings
                // Generate contact page
                Contact
                //// Generate the main blog page (a redirect)
                //Blogs (BlogListingArgs.Empty)
                //// Generate redirection pages for the old article pages
                //for (_, article) in articles do
                //    Redirect1 (fst article.Identity, article.SlugWithoutDate)
                // Generate articles
                for ((user, slug), _) in articles do
                    if String.IsNullOrEmpty user then
                        Article slug
                    else
                        UserArticle (user, slug)
                //// Generate user pages
                //for user in users do
                //    UserArticle (user, "")
                //// Generate tag/category pages
                //for category in categories do
                //    for language in languages do
                //        if
                //            List.exists (fun (_, (art: Site.Article)) ->
                //                language = Site.URL_LANG config.Value art.Language
                //                &&
                //                List.contains category art.Categories 
                //            ) articles
                //        then
                //            Category (category, language)
                //Categories
                // Generate the RSS/Atom feeds
                RSSFeed
                AtomFeed
                for user in users do
                    RSSFeedForUser user
                    AtomFeedForUser user
                //for job in jobs do
                //    Job job
                //// Generate 404 page
                //Error404
                //// Generate legal pages
                //CookiePolicy
                //TermsOfUse
                //PrivacyPolicy
                //Research
                //Consulting
                //Careers
            ]

[<assembly: Website(typeof<Website>)>]
do ()
