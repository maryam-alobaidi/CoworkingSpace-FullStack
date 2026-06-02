using CoworkingSpace.DAL;
using CoworkingSpace.Models;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CoworkingSpace.BLL
{
    public class clsPayments
    {
        public enum enMode { addNew = 0, update = 1 }
        public enMode Mode = enMode.addNew;



        public int PaymentID { get; set; }
        public int ReferenceID { get; set; }
        public string ReferenceType { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionID { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public clsPayments()
        {
            this.PaymentID = -1;
            this.ReferenceID = -1;
            this.ReferenceType = "";
            this.Amount = 0.0m;
            this.Currency = "";
            this.PaymentMethod = "";
            this.TransactionID = "";
            this.PaymentStatus = "";
            this.CreatedAt = DateTime.Now;
            this.Mode = enMode.addNew;
        }

        private clsPayments(paymentsModel model)
        {
            this.PaymentID = (int)model. PaymentID;
            this.ReferenceID = model.ReferenceID;
            this.ReferenceType = model.ReferenceType;
            this.Amount = model.Amount;
            this.Currency = model.Currency;
            this.PaymentMethod = model.PaymentMethod;
            this.TransactionID = model.TransactionID;
            this.PaymentStatus = model.PaymentStatus;
            this.CreatedAt = model.CreatedAt;

            this.Mode = enMode.update;
        }

        private async Task<bool> _AddNewPayments()
        {
            paymentsModel model = new paymentsModel
            {
                ReferenceID = this.ReferenceID,
                ReferenceType = this.ReferenceType,
                Amount = this.Amount,
                Currency = this.Currency,
                PaymentMethod = this.PaymentMethod,
                TransactionID = this.TransactionID,
                PaymentStatus = this.PaymentStatus,
                CreatedAt = this.CreatedAt
            };
            // Call DataAccess Layer
            this.PaymentID = (int)await clsPaymentsData.AddNewPayments(model);
            return (this.PaymentID != -1);
        }

        public static Task<bool> Delete(int PaymentID)
        {
            // Call DataAccess Layer
            return clsPaymentsData.DeletePayments(PaymentID);
        }

        public static clsPayments Find(int PaymentID)
        {
            paymentsModel model = new paymentsModel();
            bool IsFound = clsPaymentsData.FindByID(PaymentID, model);
            if (IsFound)
                return new clsPayments(model);
            return null;
        }

      

        public static async Task<List<paymentsModel>> GetAllPayments()
        {
            return await clsPaymentsData.GetAllPayments();
        }

        public async Task<bool> Save()
        {
            switch (Mode)
            {
                case enMode.addNew:
                    Mode = enMode.update;
                    return await _AddNewPayments();
                case enMode.update:
                    return await _UpdatePayments();
            }
            return false;
        }

        private async Task<bool> _UpdatePayments()
        {
            paymentsModel model = new paymentsModel();
            // Call DataAccess Layer
            return await clsPaymentsData.UpdatePayments(model)??false;
        }

    }

}
