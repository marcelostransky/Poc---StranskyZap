/* Conexão e reconexao com o signalr */
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/stranskyhub")
    .configureLogging(signalR.LogLevel.Information)
    .build();



function ConnectionStart() {
    connection.start().then(function () {
        HabilitarLogin();
        HabilitarCadastr();
        HabilitarConversa();
        console.info("Conectado");
    }).catch(
        function (err) {
            console.error(err.toString());
            setTimeout(ConnectionStart(), 10000);
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
    MonitorarConnectionId();
    MonitorarListaUsuario();

}
function MonitorarListaUsuario() {
    connection.invoke("ObterListaUsuario");
    connection.on("ReceberListaUsuario", function (usuarios) {
        var html = "";
        for (i = 0; i < usuarios.length; i++) {
            html += `<div class="container-user-item">
            <img src=${"/imagem/logo.png"} style="width: 10%;" />
            <div>
                <span>${usuarios[i].nome} (${usuarios[i].IsOnLine ? "OnLine" : "OffLine"})</span>
                <span class="email">${usuarios[i].email}</span>
            </div>
        </div>`
        }

        document.getElementById("users").innerHTML = html;
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

    connection.invoke("DelConnectionId", GetUsuarioLogado()).then().catch();
    sessionStorage.removeItem("Logado");
    window.location.href = "/Home/Login";
}
ConnectionStart();