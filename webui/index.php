<?php

echo "<h1>GET:</h1>\n";

foreach ($_GET as $key => $value) {
	echo $key . " =&gt; " . $value . "<br/>\n";
}

echo "<h1>PUT:</h1>\n";

foreach ($_PUT as $key => $value) {
	echo $key . " =&gt; " . $value . "<br/>\n";
}

echo "<h1>SERVER:</h1>\n";
echo "REQUEST_URI: {$_SERVER['REQUEST_URI']}<br/>\n";

foreach ($_SERVER as $key => $value) {
	if (substr($key, 0, 5) === "HTTP_") {
		echo $key . " =&gt; " . $value . "<br/>\n";
	}
}


 ?>
