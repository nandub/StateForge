<%@ Page Language="C#" %>
<script runat="server">
protected void Page_Load(object sender, EventArgs e)
{
    if (Session["Counter"] == null) { Session["Counter"] = 1; }
    else { Session["Counter"] = (int)Session["Counter"] + 1; }

    Response.Write("StateForge Counter: " + Session["Counter"]);
}
</script>
