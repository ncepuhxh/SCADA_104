/*
 * Author : ez
 * create Date : 2015/10/14
 * Describe : some iec104 protocol tools.
*/

namespace libez {

	using System.IO;
	using System.Collections;

    /// <summary>
    /// U型帧控制域枚举
    /// </summary>
	public enum Uflag : byte {
        /// <summary>
        /// 开始传输 激活
        /// </summary>
		startdt_active	= 0x07,

        /// <summary>
        /// 开始传输 确认
        /// </summary>
		startdt_confirm = 0x08,

        /// <summary>
        /// 结束传输 激活
        /// </summary>
		stopdt_active	= 0x10,

        /// <summary>
        /// 结束传输 确认
        /// </summary>
		stopdt_confirm	= 0x20,

        /// <summary>
        /// 测试 激活
        /// </summary>
		testfr_active	= 0x40,

        /// <summary>
        /// 测试 确认
        /// </summary>
		testfr_confirm	= 0x80
	};

    /// <summary>
    /// 封装I型帧的接收、发送序列号；
    /// 或者 S型帧的接收序列号。
    /// </summary>
	public class snd_rcv_number {

		 public int sent;
		 public int recvd;

		public snd_rcv_number (int _snd, int _rcv) {
			this.sent = _snd;
			this.recvd = _rcv;
		}
	}

    /// <summary>
    /// IEC-60870-5-104 protocol 工具类
    /// </summary>
	public class iec104 {

        /// <summary>
        /// 判断_frm是否是U型帧
        /// </summary>
        /// <param name="_frm">欲判断的帧</param>
        /// <param name="_start_index">帧起始标志(0x68)索引，以0开始</param>
        /// <returns></returns>
		public static bool isUframe (byte [] _frm, int _start_index = 0) {
			return _frm.Length >= 6 &&
				_frm [0 + _start_index] == 0x68 &&
				_frm [1 + _start_index] == 0x04 &&
				((_frm [2 + _start_index] & 0x03) == 0x03);
		}

        /// <summary>
        /// 判断_frm是否是S型帧
        /// </summary>
        /// <param name="_frm">欲判断的帧</param>
        /// <param name="_start_index">帧起始标志(0x64)索引，以0开始</param>
        /// <returns>如果是，返回S帧的接收序列号，否则返回 -1</returns>
		public static int isSframe (byte [] _frm, int _start_index = 0) {
			if (_frm.Length >= 6 &&
				_frm [0 + _start_index] == 0x68 &&
				_frm [1 + _start_index] == 0x04 &&
				_frm [2 + _start_index] == 0x01 &&
				_frm [3 + _start_index] == 0x00)
			{
				int recvd_num = ((int) (_frm [4 + _start_index])) >> 1;
				recvd_num |= ((int) (_frm [5 + _start_index])) << 7;
				return recvd_num;
			} else 
				return -1;
		}

        /// <summary>
        /// 解析出I格式帧的接收、发送序列号
        /// </summary>
        /// <param name="_frm">欲判断的帧</param>
        /// <param name="_start_index">帧起始标志(0x64)索引，以0开始</param>
        /// <returns>如果是，返回I帧的接收、发送序列号，否则返回null</returns>
		public static snd_rcv_number parseIframe_id (byte [] _frm, int _start_index = 0) {

			// is valid frame
			if (_frm.Length > 6 &&
				_frm [0 + _start_index] == 0x68 &&
				_frm [1 + _start_index] > 0x04)
			{
				snd_rcv_number serial = new snd_rcv_number (0, 0);
				serial.sent |= (int) (_frm [2 + _start_index]) >> 1;
				serial.sent |= ((int) (_frm [3 + _start_index])) << 7;

				serial.recvd |= (int) (_frm [4 + _start_index] >> 1);
				serial.recvd |= (int) (_frm [5 + _start_index]) << 7;
				return serial;
			} else
				return null;
		}

        public static byte[] createICallAll(snd_rcv_number _serial)
        {
            byte[] frame = new byte[] {
				0x68, 0x0e, 0, 0, 0, 0, // APCI
				0x64,  // type flag
				0x01,  // variable 
				0x06, 0x00, // transformation reason
				0x01, 0x00, // common address
				0x00, 0x00, 0x00, // information address
				0x14   // calling byte
			};
            // sent serial
            frame[2] = (byte)((_serial.sent & 0x0000007f) << 1);
            frame[3] = (byte)((_serial.sent & 0x00007f80) >> 7);

            // received serial
            frame[4] = (byte)((_serial.recvd & 0x0000007f) << 1);
            frame[5] = (byte)((_serial.recvd & 0x00007f80) >> 7);
            return frame;
        }


        public static byte[] createICallAll(int _rcv, int _snt)
        {
            byte[] frame = new byte[] {
				0x68, 0x0e, 0, 0, 0, 0, // APCI
				0x64,  // type flag
				0x01,  // variable 
				0x06, 0x00, // transformation reason
				0x01, 0x00, // common address
				0x00, 0x00, 0x00, // information address
				0x14   // calling byte
			};
            // sent serial
            frame[2] = (byte)((_snt & 0x0000007f) << 1); // lo
            frame[3] = (byte)((_snt & 0x00007f80) >> 7); // hi

            // received serial
            frame[4] = (byte)((_rcv & 0x0000007f) << 1); // lo
            frame[5] = (byte)((_rcv & 0x00007f80) >> 7); // hi
            return frame;
        }

        /// <summary>
        /// 创建S帧
        /// </summary>
        /// <param name="_rcv_serial">填充了接收序列号的snd_rcv_number实例</param>
        /// <returns></returns>
		public static byte [] create_Sframe (snd_rcv_number _rcv_serial) {
			byte [] iframe = new byte [] {
				0x68, 0x04,
				0x01, 0x00,
				(byte) ((_rcv_serial.recvd & 0x0000007F) << 1),
				(byte) ((_rcv_serial.sent & 0x00007F80) >> 7)
			};
			return iframe;
		}

        /// <summary>
        /// 创建S帧
        /// </summary>
        /// <param name="_rcv">接收序列号</param>
        /// <returns></returns>
		public static byte [] create_Sframe (int _rcv) {
			byte [] iframe = new byte [] {
				0x68, 0x04,
				0x01, 0x00,
				(byte) ((_rcv & 0x0000007F) << 1),
				(byte) ((_rcv & 0x00007F80) >> 7)
			};
			return iframe;
		}

        /// <summary>
        /// 创建U型帧
        /// </summary>
        /// <param name="_uflag">U型帧控制域</param>
        /// <returns></returns>
		public static byte [] create_Uframe (Uflag _uflag) {
			byte [] uframe = new byte [] {
				0x68, 0x04,
				// 4 octs
				(byte) ((byte) _uflag | (byte) 0x03), 
				0x00,
				0x00, 0x00
			};
			return uframe;
		}

        /// <summary>
        /// 累加S或I型帧的接收序列号域
        /// </summary>
        /// <param name="_frm">欲处理的帧</param>
        /// <param name="_start_index">帧起始标志(0x64)索引，以0开始</param>
        /// <returns></returns>
		public static byte [] add_rcv_serial (byte [] _frm, int _start_index = 0) {
			if (_frm [4 + _start_index] != 0xFE) {
				_frm [4 + _start_index] += 0x02;
			} else {
				_frm [4 + _start_index] ^= _frm [4 + _start_index]; // equals _frm [4] = 0;
				++ _frm [5 + _start_index];
			}
            return _frm;
		}

        /// <summary>
        /// 累加I型帧的发送序列号域
        /// </summary>
        /// <param name="_frm">欲处理的帧</param>
        /// <param name="_start_index">帧起始标志(0x64)索引，以0开始</param>
        /// <returns></returns>
		public static byte [] add_snt_serial (byte [] _frm, int _start_index = 0) {
			if (_frm [2 + _start_index] != 0xFE) {
				_frm [2 + _start_index] += 0x02;
			} else {
				_frm [2 + _start_index] ^= _frm [2 + _start_index]; // equals _frm [4] = 0;
				++ _frm [3 + _start_index];
			}
            return _frm;
		}

        /// <summary>
        /// 同时累加I型帧的发送和接收序列号域
        /// </summary>
        /// <param name="_frm">欲处理的帧</param>
        /// <param name="_start_index">帧起始标志(0x64)索引，以0开始</param>
        /// <returns></returns>
		public static byte [] add_both_serial (byte [] _frm, int _start_index = 0) {
			// sent 
			if (_frm [2 + _start_index] != 0xFE) {
				_frm [2 + _start_index] += 0x02;
			} else {
				_frm [2 + _start_index] ^= _frm [2 + _start_index]; // equals _frm [4] = 0;
				++ _frm [3 + _start_index];
			}

			// received
			if (_frm [4 + _start_index] != 0xFE) {
				_frm [4 + _start_index] += 0x02;
			} else {
				_frm [4 + _start_index] ^= _frm [4 + _start_index]; // equals _frm [4] = 0;
				++ _frm [5 + _start_index];
			}
            return _frm;
		}

        /// <summary>
        /// 创建U型帧
        /// </summary>
        /// <param name="_uflag">U型帧控制域</param>
        /// <returns></returns>
		public static byte [] create_Uframe (byte _uflag) {
			byte [] uframe = new byte [] {
				0x68, 0x04,
				// 4 octs
				(byte) (_uflag | (byte) 0x03), 
				0x00,
				0x00, 0x00
			};
			return uframe;
		}

        /// <summary>
        /// 解析U型帧
        /// </summary>
        /// <param name="_frm">欲解析的帧</param>
        /// <param name="_start_index">帧起始标志(0x64)索引，以0开始</param>
        /// <returns>返回U型帧控制域枚举</returns>
		public static Uflag parse_Uframe (byte [] _frm, int _start_index = 0) {
			return (Uflag) _frm [2 + _start_index]; // u flag
		}

        /// <summary>
        /// 解析S型帧
        /// </summary>
        /// <param name="_frm">欲解析的帧</param>
        /// <param name="_start_index">帧起始标志(0x64)索引，以0开始</param>
        /// <returns>若是S型帧，解析出接受序列号实例，否则返回null</returns>
		public static snd_rcv_number parse_Sframe (byte [] _frm, int _start_index = 0) {
			
			// is valid frame
			if (/*_frm.Length == 6 &&*/
				_frm [0 + _start_index] == 0x68 &&
				_frm [1 + _start_index] == 0x04 &&
				_frm [2 + _start_index] == 0x01)
			{
				snd_rcv_number serial = new snd_rcv_number (0, 0);
				serial.sent |=  (int) (_frm [2 + _start_index]) >> 1;
				serial.sent |= ((int) (_frm [3 + _start_index])) << 7;

				serial.recvd |= (int) (_frm [4 + _start_index] >> 1);
				serial.recvd |= (int) (_frm [5 + _start_index]) << 7;
				return serial;
			} else
				return null;
		}


	}
}
