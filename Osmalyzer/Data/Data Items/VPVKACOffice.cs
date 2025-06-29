﻿namespace Osmalyzer;

public class VPVKACOffice
{
    public string Name { get; }
    
    public VPVKACAddress Address { get; }
    
    public string Email { get; }
    
    public string Phone { get; }
    
    public string OpeningHours { get; }


    public VPVKACOffice(string name, VPVKACAddress address, string email, string phone, string openingHours)
    {
        Name = name;
        Address = address;
        Email = email;
        Phone = phone;
        OpeningHours = openingHours;
    }
        
        
    public string ReportString()
    {
        return
            "VPVKAC office " +
            "`" + Name + "` " +
            "at `" + Address + "`";
    }


    public record VPVKACAddress(string Name, string Location, string? Pagasts, string? Novads, string PostalCode)
    {
        public override string ToString() => ToString(false);

        public string ToString(bool full)
        {
            if (full)
                return Name +
                       ", " + Location +
                       (Pagasts != null ? ", " + Pagasts : "") +
                       (Novads != null ? ", " + Novads : "") +
                       ", " + PostalCode;
            else
                return Name + 
                       ", " + Location + 
                       ", " + PostalCode;
        }
    }
}