#define LED             (13)
#define BAUDRATE        (115200)
#define HALF_A_SECOND   (500)
#define PRINT_DELAY     (50)
//#define DEBUG_MODE

#define DEFAULT_MODE    (0)
#define START_MODE      (1)
#define CONTROL_MODE    (2)
#define SAFE_MODE       (3)
#define FULL_MODE       (4)

#define PACKET0			(0)
#define PACKET1			(1)
#define PACKET2			(2)
#define PACKET3			(3)

// -- COMMANDS -- //
#define ENTER_START_MODE    (128)
#define ENTER_CONTROL_MODE  (130)
#define ENTER_SAFE_MODE     (131)
#define ENTER_FULL_MODE     (132)
#define START_CLEAN         (135)
#define GET_SENSORS         (142)
// -------------- //


int inByte = 0;
int mode = DEFAULT_MODE;
char packet1[11] = {'\xA1', '\xA2', '\xA3', '\xA4', '\xA5', '\xA6', '\xA7', '\xA8', '\xA9', '\xAA', '\x00' };
char packet2[7] = {'\xB1', '\xB2', '\xB3', '\xB4', '\xB5', '\xB6', '\x00'};
char packet3[11] = {'\xC1', '\xC2', '\xC3', '\xC4', '\xC5', '\xC6', '\xC7', '\xC8', '\xC9', '\xCA', '\x00' };


void setup()
{
    pinMode(LED,OUTPUT);
    digitalWrite(LED, HIGH);
    Serial.begin(BAUDRATE);
    Serial1.begin(BAUDRATE);
    delay(HALF_A_SECOND);

#ifdef DEBUG_MODE
    while(!Serial) {}
    TRACE(">> Starting Roomba Simulator.");
#endif

    digitalWrite(LED, LOW);
}

void loop()
{
  
  if (Serial1.available() > 0)
  {
    digitalWrite(LED, HIGH);
    inByte = Serial1.read();
    TRACEVAL(inByte);
    
    // ~ State Machine ~ //
    if (mode == DEFAULT_MODE)
    {
    	switch (inByte)
    	{
    	case ENTER_START_MODE:
    		TRACE(">> Entering Start Mode.");
    		mode = START_MODE;
    		break;
	default:
		echo(inByte);
		break;
	}
    }
    
    else if (mode == START_MODE)
    {
    	switch (inByte)
      	{
       	case START_CLEAN:
        	TRACE(">> Clean command received.");
        	break;

       	case ENTER_CONTROL_MODE:
       		mode = CONTROL_MODE;
       		TRACE(">> Entering Control Mode");
       		break;

       	case ENTER_SAFE_MODE:
       		mode = SAFE_MODE;
       		TRACE(">> Entering Safe Mode");
       		break;

       	case ENTER_FULL_MODE:
       		mode = FULL_MODE;
       		TRACE(">> Entering Full Mode");
       		break;

       	case GET_SENSORS:
       		TRACE(">> Sensor Inquiry");
       		get_sensors();
       		break;

	default:
		TRACE(">> Unrecognized command.");
		break;

      }
      
    }
    
    else // any other mode: not implemented.
    {
      if (inByte == ENTER_START_MODE) mode = START_MODE;
      else
      {
          TRACE(">> Mode not implemented");
          echo(inByte);
      }
    }
        
    digitalWrite(LED, LOW);
  }
  
}

void echo(byte inbyte)
{
    Serial1.print((char)inbyte);
    delay(PRINT_DELAY);
}

void get_sensors()
{
    while (Serial1.available() == 0)
    {
        delay(PRINT_DELAY);
    }

    inByte = Serial1.read();
    TRACEVAL(inByte);

    switch (inByte)
    {
    case PACKET0:
    	Serial1.print(packet1);
    	Serial1.print(packet2);
    	Serial1.print(packet3);    
    	break;

    case PACKET1:
    	Serial1.print(packet1);
    	break;

    case PACKET2:
    	Serial1.print(packet2);
    	break;

    case PACKET3:
    	Serial1.print(packet3);
    	break;

    default:
    	TRACE(">> Unrecognized packet type.");
    	break;

    }
}


void TRACE(char* string)
{
#ifdef DEBUG_MODE
      Serial.println(string);
      delay(PRINT_DELAY);
#endif
}

void TRACEVAL(byte b)
{
#ifdef DEBUG_MODE
      Serial.print("RX: ");
      Serial.println((char)inByte,DEC);
      delay(PRINT_DELAY);
#endif
}
