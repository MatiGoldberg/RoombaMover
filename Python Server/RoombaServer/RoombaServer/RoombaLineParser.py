from struct import unpack

SENSOR3_FORMAT = ">cHhchH"
SENSOR2_FORMAT = ">cchh"
SENSOR1_FORMAT = ">cccccccccc"

PACKET_SIZE = {0:26, 1:10, 2:6, 3:10}
PACKET_MIN = 0
PACKET_MAX = 3

PI = 3.1415926

class RoombaLineParser:
	textline = ''
	line = ''
	data = ''
	valid = True
	counter = 0
	counter_str = "0|0"
	checksum = 0x00
	packet = 0
	error_log = []
	#----------#
	dS = 0
	dTheta = 0
	Wall = 0
	Bump = 0
	#----------#
	
	def __init__(self, Line):
		self.textline = Line
		self._parse_line()

	# --- LINE PARSER --------------------------- #
	def _parse_line(self):
		pd = self.textline.split('|')
		fd = ''.join([chr(int(a)) for a in pd])
		self.line = fd
		self.data = fd[2:-2]

		self.counter = unpack('>h',fd[:2])[0]
		self.counter_str = pd[0] + '|' + pd[1]
		self.checksum = self._calc_checksum(fd[:-1])
		self._validate_checksum()

		self.packet = ord(fd[-2])
		self._validate_packet()


	def _add_error(self, err):
		self.valid = False
		self.error_log.append(err)


	def _validate_packet(self):
		p = self.packet
		if ((p < PACKET_MIN) or (p > PACKET_MAX)):
			self._add_error('Invalid packet: {0}.'.format(p))
		else:
			if ( (len(self.line)-4) != PACKET_SIZE[p]):
				self._add_error('Improper line length for packet type {0}: {1} [{2}].'.format(p, len(self.line)-4, PACKET_SIZE[p]))


	def _validate_checksum(self):
		line_checksum = ord(self.line[-1])
		
		if (self.checksum != line_checksum):
			self._add_error('Invalid checksum')

	
	def _calc_checksum(self, fd):
		checksum = 0
		for b in fd:
			checksum = checksum + ord(b)

		checksum = checksum & 0xFF
		return checksum
	# --- LINE PARSER: END ---------------------- #

	# --- SENSOR PARSER ------------------------- #
	def parse_sensors(self):
		if (not self.valid):
			return 'Bad line.'

		if (self.packet == 0):
			nums = self._parse_packet_1(self.data[0:10]) + self._parse_packet_2(self.data[10:16]) + self._parse_packet_3(self.data[16:26])
		elif (self.packet == 1):
			nums = self._parse_packet_1(self.data)
		elif (self.packet == 2):
			nums = self._parse_packet_2(self.data)
		elif (self.packet == 3):
			nums = self._parse_packet_3(self.data)
		else:
			return 'Bad line.'

		strs = [str(x) for x in nums]
		return ','.join(strs)

	
	def _parse_packet_1(self, data):
		if (len(data) != PACKET_SIZE[1]):
			raise Exception('Bad packet.')
		
		# Sensors: bumps, wall, cliff, virtual wall, dirt, ...
		unpacked_data = unpack(SENSOR1_FORMAT, data)
		as_nums = [ord(x) for x in unpacked_data]
		
		self.Bump = as_nums[0]
		self.Wall = as_nums[1]
		return as_nums

	
	def _parse_packet_2(self, data):
		if (len(data) != PACKET_SIZE[2]):
			raise Exception('Bad packet.')

		# RC command, buttons, distance, angle
		unpacked_data = unpack(SENSOR2_FORMAT, data)
		as_nums = []
		as_nums.append(ord(unpacked_data[0]))
		as_nums.append(ord(unpacked_data[1]))
		as_nums.append(unpacked_data[2])
		as_nums.append(4.0*PI*unpacked_data[3]/(258.0)) # In Radians -- I don't know why, but 2*PI works.
		#as_nums.append(360.0*unpacked_data[3]/(258*PI)) # In Degrees
		
		self.dS = as_nums[2]
		self.dTheta = as_nums[3]
		return as_nums

	
	def _parse_packet_3(self, data):
		if (len(data) != PACKET_SIZE[3]):
			raise Exception('Bad packet.')

		# Charge state, bat voltage and so on
		unpacked_data = unpack(SENSOR3_FORMAT, data)
		as_nums = []
		as_nums.append(ord(unpacked_data[0]))
		as_nums.append(unpacked_data[1]*1.0/1000)
		as_nums.append(unpacked_data[2])
		as_nums.append(ord(unpacked_data[3]))
		as_nums.append(unpacked_data[4])
		as_nums.append(unpacked_data[5])
		return as_nums
	# --- SENSOR PARSER: END -------------------- #

	def get_vector(self):
		lst = [str(self.dS), str(self.dTheta), str(self.Bump), str(self.Wall)]
		return ','.join(lst)
