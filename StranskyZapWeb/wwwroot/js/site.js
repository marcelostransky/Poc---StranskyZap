/* Conexão e reconexao com o signalr */
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/stranskyhub")
    .configureLogging(signalR.LogLevel.Information)
    .build();


var nomeGrupo = "";
function ConnectionStart() {


    connection.start().then(function () {
        HabilitarLogin();
        HabilitarCadastr();
        HabilitarConversa();
        console.info("Conectado");
    }).catch(
        function (err) {
            if (connection.state == 0) {
                console.error(err.toString());
                setTimeout(ConnectionStart, 10000);
            }

        }
    );
}

connection.onclose(async () => { await ConnectionStart() });

var formCadastro = document.getElementById("formCadastro");
function HabilitarCadastr() {
    if (formCadastro !== null) {

        var btnCadastrar = document.getElementById("btnCadastrar");
        btnCadastrar.addEventListener("click", () => {
            var nome = document.getElementById("nome").value;
            var email = document.getElementById("email").value;
            var senha = document.getElementById("senha").value;

            var usuario = { nome, email, senha };

            if (cadastroValido())
                connection.invoke("Cadastrar", usuario).then().catch();
            else
                alert("Ops! Acho q você esqueceu um campo em branco. Corrija e tente novamente");
        });

        connection.on("ReceberCadastro", function (sucesso, usuario, msg) {

            alert(msg);

            if (sucesso) {
                document.getElementById("nome").value = '';
                document.getElementById("email").value = '';
                document.getElementById("senha").value = '';
            }

        })
    }
}
function cadastroValido() {
    var total = document.getElementById("nome").value.length +
        document.getElementById("email").value.length +
        document.getElementById("senha").value.length
    return total > 0;
}
function HabilitarLogin() {


    var formLogin = document.getElementById("formAcessar");
    if (formLogin !== null) {
        if (GetUsuarioLogado() !== null) {
            window.location.href = "/Home/Conversa";
        }
        var btnAcessar = document.getElementById("btnAcessar");
        btnAcessar.addEventListener("click", () => {
            var email = document.getElementById("email").value;
            var senha = document.getElementById("senha").value;

            var usuario = { email, senha };
            connection.invoke("Login", usuario);
        })


    }

    connection.on("ReceberLogin", (sucesso, usuario, msg) => {
        if (sucesso) {
            SetUsuarioLogado(usuario);
            window.location.href = "/Home/Conversa";
        }
        else {
            alert(msg);
        }
    })
}
var tlConversa = document.getElementById("tlConversa");
if (tlConversa !== null) {

    if (GetUsuarioLogado() === null) {
        window.location.href = "/Home/Login";
    }
}
function HabilitarConversa() {
    var tlConversa = document.getElementById("tlConversa");
    if (tlConversa !== null) {
        MonitorarConnectionId();
        MonitorarListaUsuario();
        MonitorarMensagem();
        AbrirGrupo();
        OffLineDetect();
    }

}
function OffLineDetect() {
    window.addEventListener("beforeunload", function (event) {
        connection.invoke("DelConnectionId", GetUsuarioLogado());
        event.returnValue = "Deslogar sua sessão";
        //return
    })
}
function AbrirGrupo() {
    connection.on("AbrirGrupo", (nome, mensagens) => {
        nomeGrupo = nome;
        console.info(nomeGrupo);
        var container = document.querySelector(".container-messages");
        container.innerHTML = "";
        var mensagemHtml = "";

        for (i = 0; i < mensagens.length; i++) {
            mensagemHtml += `<div class="message message-${(JSON.parse(mensagens[i].usuario).Id === GetUsuarioLogado().id) ? "right" : "left"}">
            <div class="message-head">
                <img src="/imagem/chat.png" />
                ${JSON.parse(mensagens[i].usuario).Nome}
            </div>
            <div class="message-message">
               ${mensagens[i].texto}
            </div>
 </div>

`
        }

        container.innerHTML = mensagemHtml;

    })
}
function MonitorarMensagem() {
    var btnEnviar = document.getElementById("btnEnviar");


    btnEnviar.addEventListener("click", function () {
        var msg = document.getElementById("mensagem").value;
        connection.invoke("EnviarMensagem", GetUsuarioLogado(), msg, nomeGrupo)
        document.getElementById("mensagem").value = "";
    });

    connection.on("ReceberMensagem", (mensagemReturn) => {
        var container = document.querySelector(".container-messages");
        if (nomeGrupo === mensagemReturn.nomeGrupo) {
            var mensagemHtml = `<div class="message message-${(JSON.parse(mensagemReturn.usuario).Id === GetUsuarioLogado().id) ? "right" : "left"}">
            <div class="message-head">
                <img src="/imagem/chat.png" />
                ${JSON.parse(mensagemReturn.usuario).Nome}
            </div>
            <div class="message-message">
               ${mensagemReturn.texto}
            </div> </div>`
            //var newChild = mensagemHtml
            //container.insertAdjacentHTML('beforeend', newChild);
            container.innerHTML += mensagemHtml;
            //element.appendChild(container);
        }
    });
}

function MonitorarListaUsuario() {
    connection.invoke("ObterListaUsuario");
    connection.on("ReceberListaUsuario", function (usuarios) {
        var html = "";
        for (i = 0; i < usuarios.length; i++) {
            html += `<div class="container-user-item">
            <img src=${"/imagem/logo.png"} style="width: 10%;" />
            <div>
                <span>${usuarios[i].nome.split(' ')[0]} (${usuarios[i].isOnLine ? "OnLine" : "OffLine"})</span>
                <span class="email">${usuarios[i].email}</span>
            </div>
        </div>`
        }

        document.getElementById("users").innerHTML = html;
        var container = document.getElementById("users").querySelectorAll(".container-user-item");
        for (i = 0; i < container.length; i++) {
            container[i].addEventListener("click", (event) => {
                var componente = event.target || event.srcElement;
                var emailDestinatario = componente.parentElement.querySelectorAll(".email")[0].innerText;

                connection.invoke("CriarOuAbrirGrupo", GetUsuarioLogado(), emailDestinatario);

            })
        }

    })
}
function MonitorarConnectionId() {
    var tlConversa = document.getElementById("tlConversa");
    if (tlConversa !== null) {

        var btnSair = document.getElementById("btnSair");
        btnSair.addEventListener("click", () => {
            DelUsuarioLogado();
        });

        connection.invoke("AddConnectionId", GetUsuarioLogado()).then().catch();

    }
}
function GetUsuarioLogado() {
    return JSON.parse(sessionStorage.getItem("Logado"));
}
function SetUsuarioLogado(usuario) {
    usuario.senha = "asdfyokjdasgfroiuwtbw";
    sessionStorage.setItem("Logado", JSON.stringify(usuario));
}
function DelUsuarioLogado() {

    connection.invoke("Logout", GetUsuarioLogado()).then().catch();
    sessionStorage.removeItem("Logado");
    window.location.href = "/Home/Login";
}
ConnectionStart();