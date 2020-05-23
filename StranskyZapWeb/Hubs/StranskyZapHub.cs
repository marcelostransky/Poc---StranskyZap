using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using StranskyZapWeb.DataBase;
using StranskyZapWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    _banco.Usuarios.Update(usuarioDb);
                    await _banco.SaveChangesAsync();
                }
            }

            //TODO Remover do grupo de conversa dos usuarios
        }
        public async Task Login(Usuario usuario)
        {
            if (ValidaUsuario(usuario))
            {
                var usuarioReturn = _banco.Usuarios.FirstOrDefault(u => u.Email == usuario.Email && u.Senha == usuario.Senha);
                if (usuarioReturn != null && usuarioReturn.Id > 0)
                    await Clients.Caller.SendAsync("ReceberLogin", true, usuarioReturn, "Uhuu! Vamos conversar");
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
        //var pro = promocao;

        //await Clients.Caller.SendAsync("CadastradoSucesso");  //Notificar caller -> cadastro realizado com sucesso
        //await Clients.Others.SendAsync("ReceberPromocao", promocao); //Notificar que a promoção chegou
    }
}
