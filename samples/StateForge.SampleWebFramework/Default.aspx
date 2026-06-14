<%@ Page Language="C#" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>StateForge ASP.NET Framework Sample</title>
</head>
<body>
<form id="form1" runat="server">
<script runat="server">
protected void Page_Load(object sender, EventArgs e)
{
    if (Session["Counter"] == null) { Session["Counter"] = 1; }
    else { Session["Counter"] = (int)Session["Counter"] + 1; }

    CounterLabel.Text = "StateForge session counter: " + Session["Counter"];
}
</script>
<h1>StateForge ASP.NET Framework Sample</h1>
<asp:Label ID="CounterLabel" runat="server" />
<p>Refresh the page to increment the file-backed session value.</p>
</form>
</body>
</html>
