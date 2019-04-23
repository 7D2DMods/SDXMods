public class XUiC_DialogHireInformationSDX : XUiC_DialogRespondentName
{
    public override void OnOpen()
    {
        if (base.xui.Dialog.Respondent != null)
        {
            EntityAliveSDX myEntity = base.xui.Dialog.Respondent as EntityAliveSDX;
            if (myEntity == null)
                myEntity = base.xui.Dialog.otherEntitySDX as EntityAliveSDX;
            if (myEntity)
            {
                if (myEntity.isTame(base.xui.playerUI.entityPlayer))
                {
                    return;
                }
            }
        }
        base.OnOpen();
        base.RefreshBindings(false);

    }
  
}