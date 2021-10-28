module Client

open WebSharper
open WebSharper.JavaScript

module Highlight =
    open WebSharper.HighlightJS

    [<Require(typeof<Resources.Languages.Fsharp>)>]
    [<Require(typeof<Resources.Styles.Far>)>]
    let Run() =
        JS.Document.QuerySelectorAll("code[class^=language-]").ForEach(
            (fun (node, _, _, _) -> Hljs.HighlightBlock(node)),
            JS.Undefined
        )

module Newsletter =
    open WebSharper.JQuery

    let SignUpAction () =
        let button = JS.Document.GetElementById "signup"
        button.AddEventListener("click", System.Action<Dom.Event>(fun ev -> 
            ev.PreventDefault()
            let input = JS.Document.GetElementById "newsletter-input"
            let email : string = input?value
            if email.Trim() <> "" then
                let alertList = JS.Document.GetElementById "newsletter-alert-list"
                alertList.ReplaceChildren(([||] : string []))
                input.SetAttribute("disabled", "disabled")
                let fd = FormData()
                fd.Append("email", email)
                fd.Append("type", "Blogs")
                let options =
                    RequestOptions(
                        Method = "POST",
                        Body = fd
                    )
                let responsePromise = 
                    JS.Fetch("https://api.intellifactory.com/api/newsletter", options)
                responsePromise
                    .Then(fun resp ->
                        let successMessage = JS.Document.CreateElement("div")
                        successMessage.ClassName <- "success-alert"
                        successMessage.TextContent <- "You have successfully signed up!"
                        input.RemoveAttribute("disabled")
                        alertList.AppendChild successMessage
                    )
                    .Catch(fun _ ->
                        let errorMessage = JS.Document.CreateElement("div")
                        errorMessage.ClassName <- "error-alert"
                        errorMessage.TextContent <- "Sorry, we could not sign you for the newsletter!"
                        input.RemoveAttribute("disabled")
                        alertList.AppendChild errorMessage
                    )
                    |> ignore
            else
                ()
        ))

[<SPAEntryPoint>]
let Main() =
    Highlight.Run()
    Newsletter.SignUpAction()

[<assembly:JavaScript>]
do ()
