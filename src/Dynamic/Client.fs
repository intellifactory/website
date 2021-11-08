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
    let SignUpAction () =
        let button = JS.Document.GetElementById "signup"
        let newsletterForm = JS.Document.GetElementById "newsletter-form"
        if newsletterForm <> null && newsletterForm <> JS.Undefined then
            newsletterForm.AddEventListener("submit", System.Action<Dom.Event>(fun ev ->
                ev.PreventDefault()
            ))
        if button <> null && button <> JS.Undefined then
            button.AddEventListener("click", System.Action<Dom.Event>(fun ev -> 
                ev.PreventDefault()
                let input = JS.Document.GetElementById "newsletter-input"
                let email : string = input?value
                if email.Trim() <> "" then
                    let alertList = JS.Document.GetElementById "newsletter-alert-list"
                    alertList.ReplaceChildren(([||] : string []))
                    button.SetAttribute("disabled", "disabled")
                    button.ClassList.Add("btn-disabled")
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
                            button.RemoveAttribute("disabled")
                            button.ClassList.Remove("btn-disabled")
                            alertList.AppendChild successMessage
                        )
                        .Catch(fun _ ->
                            let errorMessage = JS.Document.CreateElement("div")
                            errorMessage.ClassName <- "error-alert"
                            errorMessage.TextContent <- "Sorry, we could not sign you for the newsletter!"
                            button.RemoveAttribute("disabled")
                            button.ClassList.Remove("btn-disabled")
                            alertList.AppendChild errorMessage
                        )
                        |> ignore
                else
                    ()
            ))

module Contact =
    let SendMessageAction () =
        let button = JS.Document.GetElementById "contact-button"
        let contactForm = JS.Document.GetElementById "contact-form"
        if contactForm <> null && contactForm <> JS.Undefined then
            contactForm.AddEventListener("submit", System.Action<Dom.Event>(fun ev ->
                ev.PreventDefault()
            ))
        if button <> null && button <> JS.Undefined then
            button.AddEventListener("click", System.Action<Dom.Event>(fun ev -> 
                ev.PreventDefault()
                let emailInput = JS.Document.QuerySelector "#contact-form *[name=\"email\"]" :?> HTMLInputElement
                let subjectInput = JS.Document.QuerySelector "#contact-form *[name=\"subject\"]" :?> HTMLInputElement
                let messageInput = JS.Document.QuerySelector "#contact-form *[name=\"message\"]" :?> HTMLInputElement
                let termsInput = JS.Document.QuerySelector "#contact-form *[name=\"accept_terms\"]" :?> HTMLInputElement
                
                emailInput.ClassList.Remove("input-failed-validation")
                emailInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                subjectInput.ClassList.Remove("input-failed-validation")
                subjectInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                messageInput.ClassList.Remove("input-failed-validation")
                messageInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                termsInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Remove("text-red"))

                let email : string = emailInput.Value
                let subject : string = subjectInput.Value
                let message : string = messageInput.Value
                let terms = termsInput.Checked

                if emailInput.Validity?typeMismatch || email.Trim() = "" then
                    emailInput.ClassList.Add("input-failed-validation")
                    emailInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))
                if subject.Trim() = "" then
                    subjectInput.ClassList.Add("input-failed-validation")
                    subjectInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))
                if message.Trim() = "" then
                    messageInput.ClassList.Add("input-failed-validation")
                    messageInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))
                if not terms then
                    termsInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Add("text-red"))

                if not emailInput.Validity?typeMismatch && subject.Trim() <> "" && message.Trim() <> "" && terms then
                    let alertList = JS.Document.GetElementById "contact-alert-list"
                    alertList.ReplaceChildren(([||] : string []))
                    button.SetAttribute("disabled", "disabled")
                    button.ClassList.Add("btn-disabled")
                    let fd = FormData()
                    fd.Append("email", email)
                    fd.Append("name", subject)
                    fd.Append("message", message)
                    let options =
                        RequestOptions(
                            Method = "POST",
                            Body = fd
                        )
                    let responsePromise = 
                        JS.Fetch("https://api.intellifactory.com/api/contact", options)
                    responsePromise
                        .Then(fun resp ->
                            let modal = JS.Document.QuerySelector "#contact-form .modal"
                            let modalClose = JS.Document.QuerySelector "#contact-form .modal .modal-button"
                            modalClose.AddEventListener("click", System.Action<Dom.Event>(fun _ ->
                                let modal = JS.Document.QuerySelector "#contact-form .modal"
                                emailInput.Value <- ""
                                subjectInput.Value <- ""
                                messageInput.Value <- ""
                                termsInput.Checked <- false
                                modal.ClassList.Add "hidden"
                                button.RemoveAttribute("disabled")
                                button.ClassList.Remove("btn-disabled")
                            ))
                            modal.ClassList.Remove "hidden"
                        )
                        .Catch(fun _ ->
                            let errorMessage = JS.Document.CreateElement("div")
                            errorMessage.ClassName <- "error-alert"
                            errorMessage.TextContent <- "Sorry, we could not send your message!"
                            button.RemoveAttribute("disabled")
                            button.ClassList.Remove("btn-disabled")
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
    Contact.SendMessageAction()

[<assembly:JavaScript>]
do ()
