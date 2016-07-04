
function Send-mail([string]$Body, [string]$Subject){
#mail server configuration 
    $smtpServer   = "****"
    $smtpUser     = "****"
    $smtpPassword = "****" 
    $sslNeed      =$true #SMTP server needs SSL should set this attribute 
    $MailAddress  ="****"
    $fromName     = "****" 
    $toAddress = "****"
#create the mail message 
    $mail = New-Object System.Net.Mail.MailMessage 
#set the addresses 
    $mail.From = New-Object System.Net.Mail.MailAddress($MailAddress,$fromName) 
    $mail.To.Add($toAddress) 
#set the content 
    $mail.Subject = $Subject 
    $mail.Priority  = "High" 
    $mail.Body = $Body 
#send the message 
    $smtp = New-Object System.Net.Mail.SmtpClient -argumentList $smtpServer 
    $smtp.Credentials = New-Object System.Net.NetworkCredential -argumentList $smtpUser,$smtpPassword 
    $smtp.EnableSsl = $sslNeed; 
    try{ 
        $smtp.Send($mail) 
        echo 'Ok,Send successed!' 
    } 
    catch  
    { 
        echo 'Error!Filed!' 
    } 
}

$sqls = Get-ChildItem G:\Monitor\Services\GenereatePerDayTableSQL\* -filter *.sql

foreach($sql in $sqls) 
{
    $a = Get-Date
    sqlcmd -b -S **** -d **** -U **** -P **** -i $sql -o $sql'.log'
    if ($lastExitCode -ne 0) 
    { 
        $body = $a.ToUniversalTime().ToString() + ":" + $sql + " Failed"
        Send-Mail -Body $body -Subject $body
    }
}
