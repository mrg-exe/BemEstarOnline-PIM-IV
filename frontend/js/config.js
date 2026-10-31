// Endereço da API quando o site é aberto direto do arquivo (index.html) ou pelo Nginx do Docker (porta 8080).
// Quando o site é aberto pela própria API (http://localhost:5088 ou no Azure), a API entrega
// outra versão deste arquivo, apontando para o mesmo endereço do site (veja Program.cs).
window.BEO_CONFIG = {
  apiBase: 'http://localhost:5088/api'
};
