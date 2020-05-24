using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StranskyZapWeb.DataBase;
using StranskyZapWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StranskyZapWeb.Hubs
{
    public class StranskyZapHub : Hub
    {
        private BancoContext _banco;
        public StranskyZapHub(BancoContext banco)
        {
            _banco = banco;
        }

        /*
         * Banco
         * Queue/Sheduler
         * Notificar o Usuario (SignalR)
         * 
    
         */
        public async Task ObterListaUsuario()
        {
            var usuarios = _banco.Usuarios.ToList();
            await Clients.Caller.SendAsync("ReceberListaUsuario", usuarios);
        }
        public async Task AddConnectionId(Usuario usuario)
        {
            var connectionIdCorrente = Context.ConnectionId;
            var usuarioDb = _banco.Usuarios.Find(usuario.Id);
            List<string> lista = null;
            if (usuarioDb.ConnectionId != null && usuarioDb.ConnectionId.Length > 0)
            {
                lista = new List<string>();
                lista = JsonConvert.DeserializeObject<List<string>>(usuarioDb.ConnectionId);
                if (!lista.Contains(connectionIdCorrente))
                {
                    lista.Add(connectionIdCorrente);
                }
                usuarioDb.IsOnLine = true;

            }
            else
            {
                lista = new List<string>();
                lista.Add(connectionIdCorrente);
            }

            usuarioDb.ConnectionId = JsonConvert.SerializeObject(lista);
            _banco.Usuarios.Update(usuarioDb);
            await _banco.SaveChangesAsync();
            //TODO Adicionar ao grupo de conversa dos usuarios
            var grupos = _banco.Grupos.Where(x => x.Usuarios.Contains(usuarioDb.Email));
            foreach (var connectionId in lista)
            {
                foreach (var grupo in grupos)
                {
                    await Groups.AddToGroupAsync(connectionId, grupo.Nome);
                }

            }

            await NotificarMudancaNaListaDeUsuario();
        }


        public async Task DelConnectionId(Usuario usuario)
        {
            var connectionIdCorrente = Context.ConnectionId;
            var usuarioDb = _banco.Usuarios.Find(usuario.Id);
            List<string> lista = null;
            if (usuarioDb.ConnectionId != null)
            {
                lista = JsonConvert.DeserializeObject<List<string>>(usuarioDb.ConnectionId);
                if (lista.Contains(connectionIdCorrente))
                {
                    lista.Remove(connectionIdCorrente);
                    usuarioDb.ConnectionId = JsonConvert.SerializeObject(lista);
                    if (lista.Count <= 0)
                    {
                        usuarioDb.IsOnLine = false;

                    }
                    _banco.Usuarios.Update(usuarioDb);
                    await _banco.SaveChangesAsync();
                    //await AddConnectionId(usuarioDb);

                }

                //TODO Remover do grupo de conversa dos usuarios
                var grupos = _banco.Grupos.Where(x => x.Usuarios.Contains(usuarioDb.Email));
                foreach (var connectionId in lista)
                {
                    foreach (var grupo in grupos)
                    {
                        await Groups.RemoveFromGroupAsync(connectionId, grupo.Nome);
                    }

                }

                await NotificarMudancaNaListaDeUsuario();
            }


        }

        public async Task Logout(Usuario usuario)
        {
            var usuarioDb = _banco.Usuarios.Find(usuario.Id);
            usuarioDb.IsOnLine = false;
            _banco.Usuarios.Update(usuarioDb);
            _banco.SaveChanges();
            await DelConnectionId(usuarioDb);
        }

        public async Task NotificarMudancaNaListaDeUsuario()
        {
            var usuarios = _banco.Usuarios.ToList();
            await Clients.All.SendAsync("ReceberListaUsuario", usuarios);
        }
        public async Task Login(Usuario usuario)
        {
            if (ValidaUsuario(usuario))
            {
                var usuarioDb = _banco.Usuarios.FirstOrDefault(u => u.Email == usuario.Email && u.Senha == usuario.Senha);
                if (usuarioDb != null && usuarioDb.Id > 0)
                {
                    await Clients.Caller.SendAsync("ReceberLogin", true, usuarioDb, "Uhuu! Vamos conversar");
                    usuarioDb.IsOnLine = true;
                    _banco.Usuarios.Update(usuarioDb);
                    _banco.SaveChanges();

                    var usuarios = _banco.Usuarios.ToList();
                    await NotificarMudancaNaListaDeUsuario();
                }
                else
                    await Clients.Caller.SendAsync("ReceberLogin", false, null, ":-( Ops! Algo está errado. Por favor, verifique seu email ou senha e tente novamente");
            }
        }

        private bool ValidaUsuario(Usuario usuario)
        {
            return (!string.IsNullOrEmpty(usuario.Email) && !string.IsNullOrEmpty(usuario.Senha));
        }

        public async Task Cadastrar(Usuario usuario)
        {
            var IsExistUsuario = _banco.Usuarios.Where(a => a.Email == usuario.Email).Count() > 0 ? true : false;
            if (IsExistUsuario)
            {
                await Clients.Caller.SendAsync("ReceberCadastro", false, null, "Email já cadastrado");
            }
            else
            {
                _banco.Usuarios.Add(usuario);
                _banco.SaveChanges();
                await Clients.Caller.SendAsync("ReceberCadastro", true, usuario, "Oba!!!! Você agora é um StranskyZap");
            }
        }

        public async Task CriarOuAbrirGrupo(Usuario usuario, string emailDestinatario)
        {
            var grupoNome = CriarNomeGrupo(usuario.Email, emailDestinatario);
            Grupo grupo = _banco.Grupos.FirstOrDefault(x => x.Nome.Equals(grupoNome));
            var user = _banco.Usuarios.Find(usuario.Id);
            if (grupo == null)
            {
                grupo = new Grupo();
                grupo.Nome = grupoNome;
                grupo.Usuarios = JsonConvert.SerializeObject(new List<string> { usuario.Email,
                emailDestinatario});
                _banco.Grupos.Add(grupo);
                await _banco.SaveChangesAsync();
            }

            //Criar grupo com conectionId
            var usuarios = new List<Usuario>
              {
                  user, _banco.Usuarios.FirstOrDefault(x=>x.Email.Equals(emailDestinatario))
              };
            //Adicionar Connections id aos grupos
            foreach (var item in usuarios)
            {
                if (!string.IsNullOrEmpty(item.ConnectionId))
                {
                    var connectionsId = JsonConvert.DeserializeObject<List<string>>(item.ConnectionId);

                    foreach (var connectionId in connectionsId)
                    {
                        try
                        {
                            await Groups.AddToGroupAsync(connectionId, grupoNome);
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }

                    }
                }
            }
            var mensagens = _banco.Mensagens.Where(x => x.NomeGrupo.Equals(grupoNome)).OrderBy(d => d.DataCriacao).ToList();
            await Clients.Caller.SendAsync("AbrirGrupo", grupoNome, mensagens);

        }

        public async Task EnviarMensagem(Usuario usuario, string msg, string nomeGrupo)
        {
            Grupo grupo = _banco.Grupos.FirstOrDefault(x => x.Nome.Equals(nomeGrupo));


            //Validar se usuario pertence ao grupo

            if (!grupo.Usuarios.Contains(usuario.Email))
                throw new Exception("Usuario nao pertence ao grupo");
            //Salvar a mensagem enviada
            Mensagem mensagemReturn = new Mensagem()
            {
                NomeGrupo = grupo.Nome,
                Texto = msg,
                UsuarioObj = usuario,
                UsuarioId = usuario.Id,
                Usuario = JsonConvert.SerializeObject(usuario),
                DataCriacao = DateTime.Now

            };
            _banco.Mensagens.Add(mensagemReturn);
            _banco.SaveChanges();
            //Avisar aos usuarios do grupo que existe uma nova mensagem
            await Clients.Group(mensagemReturn.NomeGrupo).SendAsync("ReceberMensagem", mensagemReturn);



        }

        private string CriarNomeGrupo(string email, string emailDestinatario)
        {
            var listaOrdenada = new List<string> { email, emailDestinatario }.OrderBy(x => x).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (var item in listaOrdenada)
            {
                sb.Append(item);
            }

            return sb.ToString();
        }



        //var pro = promocao;

        //await Clients.Caller.SendAsync("CadastradoSucesso");  //Notificar caller -> cadastro realizado com sucesso
        //await Clients.Others.SendAsync("ReceberPromocao", promocao); //Notificar que a promoção chegou
    }
}
