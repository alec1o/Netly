---
sidebar_position: 3
---

# SSL/TLS

## Client
For use/enable ``SSL/TLS`` on ``Netly`` with ``TcpClient`` instance use code bellow.

<return>Warning</return> When you enable ``SSL/TLS`` and your server not use ``SSL/TLS`` you connection will be closed.

- Default config
	```cs
	using Netly;

	TcpClient client = new TcpClient(framing: true);

	// Enable SSL/TLS connection.
	client.UseEncryption(true);
	```

- Custom validatiion
	```cs
	using Netly;

	TcpClient client = new TcpClient(framing: true);

	// Enable SSL/TLS connection.
	client.UseEncryption(true, Validator);

	bool Validator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{

	#if false
		// Default behaviour
		return true;
	#endif

		// Custom validation
		// Source: https://learn.microsoft.com/dotnet/api/system.net.security.sslstream

		if (sslPolicyErrors == SslPolicyErrors.None) {
			// Valid server
            return true;
        }

        Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

        // refuse connection
        return false;

	}
	```

## Server
For use/enable ``SSL/TLS`` on ``Netly`` with ``TcpServer`` instance use code bellow.

<return>Warning</return> When you enable ``SSL/TLS`` and your client not use ``SSL/TLS`` you client connection will be closed.


```cs
    // Warning: See about generate pfx on SSL/TLS page now we will see about startup this!
    // Warning: Convert pfx file for bytes only using UTF8 for prevent erros

    byte[] pfx = <class>.<method-get-pfx-buffer>();
    string pfxPassword = <class>.<method-get-pfx-password>();

    // Enable SSL/TLS
    TcpServer.UseEncryption(pfx, pfxPassword, SslProtocols.Tls12); // TLS v1.2

#if false
	If password or PFX buffer is invalid you will receive error message on <TcpServer.OnError(Action<Exception> callback)>
#endif
```

## Create PFX (PKCS #12)
* Requirement ``OpenSSL``
	- linux: Use package manager
		- Ubuntu: ``sudo apt install openssl`` or ``sudo apt install libssl-dev``
	- Windows:
		- Download windows: https://wiki.openssl.org/index.php/Binaries
		- Add OpenSSL folder on ``path`` (``Environment Variables``)
	- Generate ``PFX (PKCS #12)``: https://www.ibm.com/docs/en/api-connect/10.0.x?topic=overview-generating-self-signed-certificate-using-openssl
