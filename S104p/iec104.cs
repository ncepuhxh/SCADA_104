/*
 * Author : ez
 * create Date : 2015/10/14
 * Describe : some iec104 protocol tools.
*/

namespace libez {

	using System.IO;
	using System.Collections;

    /// <summary>
    /// U��֡������ö��
    /// </summary>
	public enum Uflag : byte {
        /// <summary>
        /// ��ʼ���� ����
        /// </summary>
		startdt_active	= 0x07,

        /// <summary>
        /// ��ʼ���� ȷ��
        /// </summary>
		startdt_confirm = 0x08,

        /// <summary>
        /// �������� ����
        /// </summary>
		stopdt_active	= 0x10,

        /// <summary>
        /// �������� ȷ��
        /// </summary>
		stopdt_confirm	= 0x20,

        /// <summary>
        /// ���� ����
        /// </summary>
		testfr_active	= 0x40,

        /// <summary>
        /// ���� ȷ��
        /// </summary>
		testfr_confirm	= 0x80
	};

    /// <summary>
    /// ��װI��֡�Ľ��ա��������кţ�
    /// ���� S��֡�Ľ������кš�
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
    /// IEC-60870-5-104 protocol ������
    /// </summary>
	public class iec104 {

        /// <summary>
        /// �ж�_frm�Ƿ���U��֡
        /// </summary>
        /// <param name="_frm">���жϵ�֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x68)��������0��ʼ</param>
        /// <returns></returns>
		public static bool isUframe (byte [] _frm, int _start_index = 0) {
			return _frm.Length >= 6 &&
				_frm [0 + _start_index] == 0x68 &&
				_frm [1 + _start_index] == 0x04 &&
				((_frm [2 + _start_index] & 0x03) == 0x03);
		}

        /// <summary>
        /// �ж�_frm�Ƿ���S��֡
        /// </summary>
        /// <param name="_frm">���жϵ�֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x64)��������0��ʼ</param>
        /// <returns>����ǣ�����S֡�Ľ������кţ����򷵻� -1</returns>
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
        /// ������I��ʽ֡�Ľ��ա��������к�
        /// </summary>
        /// <param name="_frm">���жϵ�֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x64)��������0��ʼ</param>
        /// <returns>����ǣ�����I֡�Ľ��ա��������кţ����򷵻�null</returns>
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
        /// ����S֡
        /// </summary>
        /// <param name="_rcv_serial">����˽������кŵ�snd_rcv_numberʵ��</param>
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
        /// ����S֡
        /// </summary>
        /// <param name="_rcv">�������к�</param>
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
        /// ����U��֡
        /// </summary>
        /// <param name="_uflag">U��֡������</param>
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
        /// �ۼ�S��I��֡�Ľ������к���
        /// </summary>
        /// <param name="_frm">�������֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x64)��������0��ʼ</param>
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
        /// �ۼ�I��֡�ķ������к���
        /// </summary>
        /// <param name="_frm">�������֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x64)��������0��ʼ</param>
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
        /// ͬʱ�ۼ�I��֡�ķ��ͺͽ������к���
        /// </summary>
        /// <param name="_frm">�������֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x64)��������0��ʼ</param>
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
        /// ����U��֡
        /// </summary>
        /// <param name="_uflag">U��֡������</param>
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
        /// ����U��֡
        /// </summary>
        /// <param name="_frm">��������֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x64)��������0��ʼ</param>
        /// <returns>����U��֡������ö��</returns>
		public static Uflag parse_Uframe (byte [] _frm, int _start_index = 0) {
			return (Uflag) _frm [2 + _start_index]; // u flag
		}

        /// <summary>
        /// ����S��֡
        /// </summary>
        /// <param name="_frm">��������֡</param>
        /// <param name="_start_index">֡��ʼ��־(0x64)��������0��ʼ</param>
        /// <returns>����S��֡���������������к�ʵ�������򷵻�null</returns>
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
